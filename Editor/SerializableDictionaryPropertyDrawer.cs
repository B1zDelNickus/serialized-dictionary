using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hag.SerializableDictionary.Runtime;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hag.SerializableDictionary.Editor
{
    [CustomPropertyDrawer(typeof(SerializableDictionaryBase), true)]
#if NET_4_6 || NET_STANDARD_2_0
    [CustomPropertyDrawer(typeof(SerializableHashSetBase), true)]
#endif
    public class SerializableDictionaryPropertyDrawer : PropertyDrawer
    {
        private const string KeysFieldName = "keys";
        private const string ValuesFieldName = "values";
        private const float IndentWidth = 15f;

        private static readonly GUIContent IconPlus = IconContent("Toolbar Plus", "Add entry");
        private static readonly GUIContent IconMinus = IconContent("Toolbar Minus", "Remove entry");

        private static readonly GUIContent WarningIconConflict =
            IconContent("console.warnicon.sml", "Conflicting key, this entry will be lost");

        private static readonly GUIContent WarningIconOther = IconContent("console.infoicon.sml", "Conflicting key");
        private static readonly GUIContent WarningIconNull = IconContent("console.warnicon.sml", "Null key, this entry will be lost");
        private static readonly GUIStyle ButtonStyle = GUIStyle.none;
        private static readonly GUIContent STempContent = new GUIContent();


        public class ConflictState
        {
            public object conflictKey;
            public object conflictValue;
            public int conflictIndex = -1;
            public int conflictOtherIndex = -1;
            public bool conflictKeyPropertyExpanded;
            public bool conflictValuePropertyExpanded;
            public float conflictLineHeight;
        }

        private struct PropertyIdentity
        {
            public PropertyIdentity(SerializedProperty property)
            {
                instance = property.serializedObject.targetObject;
                propertyPath = property.propertyPath;
            }

            // ReSharper disable once NotAccessedField.Local
            public UnityEngine.Object instance;
            // ReSharper disable once NotAccessedField.Local
            public string propertyPath;
        }

        private static readonly Dictionary<PropertyIdentity, ConflictState> ConflictStateDict = new();

        private enum Action
        {
            None,
            Add,
            Remove
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);

            var buttonAction = Action.None;
            var buttonActionIndex = 0;

            var keyArrayProperty = property.FindPropertyRelative(KeysFieldName);
            var valueArrayProperty = property.FindPropertyRelative(ValuesFieldName);

            var conflictState = GetConflictState(property);

            if (conflictState.conflictIndex != -1)
            {
                keyArrayProperty.InsertArrayElementAtIndex(conflictState.conflictIndex);
                var keyProperty = keyArrayProperty.GetArrayElementAtIndex(conflictState.conflictIndex);
                SetPropertyValue(keyProperty, conflictState.conflictKey);
                keyProperty.isExpanded = conflictState.conflictKeyPropertyExpanded;

                if (valueArrayProperty != null)
                {
                    valueArrayProperty.InsertArrayElementAtIndex(conflictState.conflictIndex);
                    var valueProperty = valueArrayProperty.GetArrayElementAtIndex(conflictState.conflictIndex);
                    SetPropertyValue(valueProperty, conflictState.conflictValue);
                    valueProperty.isExpanded = conflictState.conflictValuePropertyExpanded;
                }
            }

            var buttonWidth = ButtonStyle.CalcSize(IconPlus).x;

            var labelPosition = position;
            labelPosition.height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                labelPosition.xMax -= ButtonStyle.CalcSize(IconPlus).x;

            EditorGUI.PropertyField(labelPosition, property, label, false);
            // property.isExpanded = EditorGUI.Foldout(labelPosition, property.isExpanded, label);
            if (property.isExpanded)
            {
                var buttonPosition = position;
                buttonPosition.xMin = buttonPosition.xMax - buttonWidth;
                buttonPosition.height = EditorGUIUtility.singleLineHeight;
                EditorGUI.BeginDisabledGroup(conflictState.conflictIndex != -1);
                if (GUI.Button(buttonPosition, IconPlus, ButtonStyle))
                {
                    buttonAction = Action.Add;
                    buttonActionIndex = keyArrayProperty.arraySize;
                }

                EditorGUI.EndDisabledGroup();

                EditorGUI.indentLevel++;
                var linePosition = position;
                linePosition.y += EditorGUIUtility.singleLineHeight;
                linePosition.xMax -= buttonWidth;

                foreach (var entry in EnumerateEntries(keyArrayProperty, valueArrayProperty))
                {
                    var keyProperty = entry.keyProperty;
                    var valueProperty = entry.valueProperty;
                    var i = entry.index;

                    var lineHeight = DrawKeyValueLine(keyProperty, valueProperty, linePosition, i);

                    buttonPosition = linePosition;
                    buttonPosition.x = linePosition.xMax;
                    buttonPosition.height = EditorGUIUtility.singleLineHeight;
                    if (GUI.Button(buttonPosition, IconMinus, ButtonStyle))
                    {
                        buttonAction = Action.Remove;
                        buttonActionIndex = i;
                    }

                    if (i == conflictState.conflictIndex && conflictState.conflictOtherIndex == -1)
                    {
                        var iconPosition = linePosition;
                        iconPosition.size = ButtonStyle.CalcSize(WarningIconNull);
                        GUI.Label(iconPosition, WarningIconNull);
                    }
                    else if (i == conflictState.conflictIndex)
                    {
                        var iconPosition = linePosition;
                        iconPosition.size = ButtonStyle.CalcSize(WarningIconConflict);
                        GUI.Label(iconPosition, WarningIconConflict);
                    }
                    else if (i == conflictState.conflictOtherIndex)
                    {
                        var iconPosition = linePosition;
                        iconPosition.size = ButtonStyle.CalcSize(WarningIconOther);
                        GUI.Label(iconPosition, WarningIconOther);
                    }


                    linePosition.y += lineHeight;
                }

                EditorGUI.indentLevel--;
            }

            switch (buttonAction)
            {
                case Action.Add:
                {
                    keyArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
                    if (valueArrayProperty != null)
                        valueArrayProperty.InsertArrayElementAtIndex(buttonActionIndex);
                    break;
                }
                case Action.Remove:
                {
                    DeleteArrayElementAtIndex(keyArrayProperty, buttonActionIndex);
                    if (valueArrayProperty != null)
                        DeleteArrayElementAtIndex(valueArrayProperty, buttonActionIndex);
                    break;
                }
                case Action.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            conflictState.conflictKey = null;
            conflictState.conflictValue = null;
            conflictState.conflictIndex = -1;
            conflictState.conflictOtherIndex = -1;
            conflictState.conflictLineHeight = 0f;
            conflictState.conflictKeyPropertyExpanded = false;
            conflictState.conflictValuePropertyExpanded = false;

            foreach (var entry1 in EnumerateEntries(keyArrayProperty, valueArrayProperty))
            {
                var keyProperty1 = entry1.keyProperty;
                var i = entry1.index;
                var keyProperty1Value = GetPropertyValue(keyProperty1);

                if (keyProperty1Value == null)
                {
                    var valueProperty1 = entry1.valueProperty;
                    SaveProperty(keyProperty1, valueProperty1, i, -1, conflictState);
                    DeleteArrayElementAtIndex(keyArrayProperty, i);
                    if (valueArrayProperty != null)
                        DeleteArrayElementAtIndex(valueArrayProperty, i);

                    break;
                }


                foreach (var entry2 in EnumerateEntries(keyArrayProperty, valueArrayProperty, i + 1))
                {
                    var keyProperty2 = entry2.keyProperty;
                    var j = entry2.index;
                    var keyProperty2Value = GetPropertyValue(keyProperty2);

                    if (!ComparePropertyValues(keyProperty1Value, keyProperty2Value)) continue;
                    var valueProperty2 = entry2.valueProperty;
                    SaveProperty(keyProperty2, valueProperty2, j, i, conflictState);
                    DeleteArrayElementAtIndex(keyArrayProperty, j);
                    if (valueArrayProperty != null)
                        DeleteArrayElementAtIndex(valueArrayProperty, j);

                    goto breakLoops;
                }
            }

            breakLoops:

            EditorGUI.EndProperty();
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static float DrawKeyValueLine(SerializedProperty keyProperty, SerializedProperty valueProperty,
            Rect linePosition, int index)
        {
            var keyCanBeExpanded = CanPropertyBeExpanded(keyProperty);

            if (valueProperty != null)
            {
                var valueCanBeExpanded = CanPropertyBeExpanded(valueProperty);

                if (!keyCanBeExpanded && valueCanBeExpanded)
                {
                    return DrawKeyValueLineExpand(keyProperty, valueProperty, linePosition);
                }

                var keyLabel = keyCanBeExpanded ? ("Key " + index.ToString()) : "";
                var valueLabel = valueCanBeExpanded ? ("Value " + index.ToString()) : "";
                return DrawKeyValueLineSimple(keyProperty, valueProperty, keyLabel, valueLabel, linePosition);
            }

            if (!keyCanBeExpanded)
            {
                return DrawKeyLine(keyProperty, linePosition, null);
            }

            {
                var keyLabel = $"{ObjectNames.NicifyVariableName(keyProperty.type)} {index}";
                return DrawKeyLine(keyProperty, linePosition, keyLabel);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static float DrawKeyValueLineSimple(SerializedProperty keyProperty, SerializedProperty valueProperty,
            string keyLabel, string valueLabel, Rect linePosition)
        {
            var labelWidth = EditorGUIUtility.labelWidth;
            var labelWidthRelative = labelWidth / linePosition.width;

            var keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var keyPosition = linePosition;
            keyPosition.height = keyPropertyHeight;
            keyPosition.width = labelWidth - IndentWidth;
            EditorGUIUtility.labelWidth = keyPosition.width * labelWidthRelative;
            EditorGUI.PropertyField(keyPosition, keyProperty, TempContent(keyLabel), true);

            var valuePropertyHeight = EditorGUI.GetPropertyHeight(valueProperty);
            var valuePosition = linePosition;
            valuePosition.height = valuePropertyHeight;
            valuePosition.xMin += labelWidth;
            EditorGUIUtility.labelWidth = valuePosition.width * labelWidthRelative;
            EditorGUI.indentLevel--;
            EditorGUI.PropertyField(valuePosition, valueProperty, TempContent(valueLabel), true);
            EditorGUI.indentLevel++;

            EditorGUIUtility.labelWidth = labelWidth;

            return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static float DrawKeyValueLineExpand(SerializedProperty keyProperty, SerializedProperty valueProperty,
            Rect linePosition)
        {
            var labelWidth = EditorGUIUtility.labelWidth;

            var keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var keyPosition = linePosition;
            keyPosition.height = keyPropertyHeight;
            keyPosition.width = labelWidth - IndentWidth;
            EditorGUI.PropertyField(keyPosition, keyProperty, GUIContent.none, true);

            var valuePropertyHeight = EditorGUI.GetPropertyHeight(valueProperty);
            var valuePosition = linePosition;
            valuePosition.height = valuePropertyHeight;
            EditorGUI.PropertyField(valuePosition, valueProperty, GUIContent.none, true);

            EditorGUIUtility.labelWidth = labelWidth;

            return Mathf.Max(keyPropertyHeight, valuePropertyHeight);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static float DrawKeyLine(SerializedProperty keyProperty, Rect linePosition, string keyLabel)
        {
            var keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var keyPosition = linePosition;
            keyPosition.height = keyPropertyHeight;
            keyPosition.width = linePosition.width;

            var keyLabelContent = keyLabel != null ? TempContent(keyLabel) : GUIContent.none;
            EditorGUI.PropertyField(keyPosition, keyProperty, keyLabelContent, true);

            return keyPropertyHeight;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static bool CanPropertyBeExpanded(SerializedProperty property)
        {
            // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
            switch (property.propertyType)
            {
                case SerializedPropertyType.Generic:
                case SerializedPropertyType.Vector4:
                case SerializedPropertyType.Quaternion:
                    return true;
                default:
                    return false;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void SaveProperty(SerializedProperty keyProperty, SerializedProperty valueProperty, int index,
            int otherIndex, ConflictState conflictState)
        {
            conflictState.conflictKey = GetPropertyValue(keyProperty);
            conflictState.conflictValue = valueProperty != null ? GetPropertyValue(valueProperty) : null;
            var keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            var valuePropertyHeight = valueProperty != null ? EditorGUI.GetPropertyHeight(valueProperty) : 0f;
            var lineHeight = Mathf.Max(keyPropertyHeight, valuePropertyHeight);
            conflictState.conflictLineHeight = lineHeight;
            conflictState.conflictIndex = index;
            conflictState.conflictOtherIndex = otherIndex;
            conflictState.conflictKeyPropertyExpanded = keyProperty.isExpanded;
            conflictState.conflictValuePropertyExpanded = valueProperty?.isExpanded ?? false;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var propertyHeight = EditorGUIUtility.singleLineHeight;

            if (!property.isExpanded) return propertyHeight;
            var keysProperty = property.FindPropertyRelative(KeysFieldName);
            var valuesProperty = property.FindPropertyRelative(ValuesFieldName);

            propertyHeight += (from entry in EnumerateEntries(keysProperty, valuesProperty) 
                let keyProperty = entry.keyProperty 
                let valueProperty = entry.valueProperty 
                let keyPropertyHeight = EditorGUI.GetPropertyHeight(keyProperty) 
                let valuePropertyHeight = valueProperty != null ? 
                    EditorGUI.GetPropertyHeight(valueProperty) :
                    0f select Mathf.Max(keyPropertyHeight, valuePropertyHeight)).Sum();

            var conflictState = GetConflictState(property);

            if (conflictState.conflictIndex != -1)
            {
                propertyHeight += conflictState.conflictLineHeight;
            }

            return propertyHeight;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        private static ConflictState GetConflictState(SerializedProperty property)
        {
            var propId = new PropertyIdentity(property);
            if (ConflictStateDict.TryGetValue(propId, out var conflictState)) return conflictState;
            conflictState = new ConflictState();
            ConflictStateDict.Add(propId, conflictState);

            return conflictState;
        }

        private static readonly Dictionary<SerializedPropertyType, PropertyInfo> SerializedPropertyValueAccessorsDict;

        static SerializableDictionaryPropertyDrawer()
        {
            var serializedPropertyValueAccessorsNameDict =
                new Dictionary<SerializedPropertyType, string>()
                {
                    {SerializedPropertyType.Integer, "intValue"},
                    {SerializedPropertyType.Boolean, "boolValue"},
                    {SerializedPropertyType.Float, "floatValue"},
                    {SerializedPropertyType.String, "stringValue"},
                    {SerializedPropertyType.Color, "colorValue"},
                    {SerializedPropertyType.ObjectReference, "objectReferenceValue"},
                    {SerializedPropertyType.LayerMask, "intValue"},
                    {SerializedPropertyType.Enum, "intValue"},
                    {SerializedPropertyType.Vector2, "vector2Value"},
                    {SerializedPropertyType.Vector3, "vector3Value"},
                    {SerializedPropertyType.Vector4, "vector4Value"},
                    {SerializedPropertyType.Rect, "rectValue"},
                    {SerializedPropertyType.ArraySize, "intValue"},
                    {SerializedPropertyType.Character, "intValue"},
                    {SerializedPropertyType.AnimationCurve, "animationCurveValue"},
                    {SerializedPropertyType.Bounds, "boundsValue"},
                    {SerializedPropertyType.Quaternion, "quaternionValue"},
                };
            var serializedPropertyType = typeof(SerializedProperty);

            SerializedPropertyValueAccessorsDict = new Dictionary<SerializedPropertyType, PropertyInfo>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;

            foreach (var kvp in serializedPropertyValueAccessorsNameDict)
            {
                var propertyInfo = serializedPropertyType.GetProperty(kvp.Value, flags);
                SerializedPropertyValueAccessorsDict.Add(kvp.Key, propertyInfo);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static GUIContent IconContent(string name, string tooltip)
        {
            var builtinIcon = EditorGUIUtility.IconContent(name);
            return new GUIContent(builtinIcon.image, tooltip);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static GUIContent TempContent(string text)
        {
            STempContent.text = text;
            return STempContent;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void DeleteArrayElementAtIndex(SerializedProperty arrayProperty, int index)
        {
            var property = arrayProperty.GetArrayElementAtIndex(index);
            // if(arrayProperty.arrayElementType.StartsWith("PPtr<$"))
            if (property.propertyType == SerializedPropertyType.ObjectReference)
            {
                property.objectReferenceValue = null;
            }

            arrayProperty.DeleteArrayElementAtIndex(index);
        }

        public static object GetPropertyValue(SerializedProperty p)
        {
            if (SerializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out var propertyInfo))
            {
                return propertyInfo.GetValue(p, null);
            }

            return p.isArray ? GetPropertyValueArray(p) : GetPropertyValueGeneric(p);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void SetPropertyValue(SerializedProperty p, object v)
        {
            if (SerializedPropertyValueAccessorsDict.TryGetValue(p.propertyType, out var propertyInfo))
            {
                propertyInfo.SetValue(p, v, null);
            }
            else
            {
                if (p.isArray)
                    SetPropertyValueArray(p, v);
                else
                    SetPropertyValueGeneric(p, v);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static object GetPropertyValueArray(SerializedProperty property)
        {
            var array = new object[property.arraySize];
            for (var i = 0; i < property.arraySize; i++)
            {
                var item = property.GetArrayElementAtIndex(i);
                array[i] = GetPropertyValue(item);
            }

            return array;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static object GetPropertyValueGeneric(SerializedProperty property)
        {
            var dict = new Dictionary<string, object>();
            var iterator = property.Copy();
            if (!iterator.Next(true)) return dict;
            var end = property.GetEndProperty();
            do
            {
                var name = iterator.name;
                var value = GetPropertyValue(iterator);
                dict.Add(name, value);
            } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);

            return dict;
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void SetPropertyValueArray(SerializedProperty property, object v)
        {
            var array = (object[]) v;
            property.arraySize = array.Length;
            for (var i = 0; i < property.arraySize; i++)
            {
                var item = property.GetArrayElementAtIndex(i);
                SetPropertyValue(item, array[i]);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static void SetPropertyValueGeneric(SerializedProperty property, object v)
        {
            var dict = (Dictionary<string, object>) v;
            var iterator = property.Copy();
            if (!iterator.Next(true)) return;
            var end = property.GetEndProperty();
            do
            {
                var name = iterator.name;
                SetPropertyValue(iterator, dict[name]);
            } while (iterator.Next(false) && iterator.propertyPath != end.propertyPath);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static bool ComparePropertyValues(object value1, object value2)
        {
            if (value1 is Dictionary<string, object> dict1 && value2 is Dictionary<string, object> dict2)
            {
                return CompareDictionaries(dict1, dict2);
            }

            return Equals(value1, value2);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static bool CompareDictionaries(Dictionary<string, object> dict1, Dictionary<string, object> dict2)
        {
            if (dict1.Count != dict2.Count)
                return false;

            foreach (var (key1, value1) in dict1)
            {
                if (!dict2.TryGetValue(key1, out var value2))
                    return false;

                if (!ComparePropertyValues(value1, value2))
                    return false;
            }

            return true;
        }

        public struct EnumerationEntry
        {
            public readonly SerializedProperty keyProperty;
            public readonly SerializedProperty valueProperty;
            public readonly int index;

            public EnumerationEntry(SerializedProperty keyProperty, SerializedProperty valueProperty, int index)
            {
                this.keyProperty = keyProperty;
                this.valueProperty = valueProperty;
                this.index = index;
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public static IEnumerable<EnumerationEntry> EnumerateEntries(SerializedProperty keyArrayProperty,
            SerializedProperty valueArrayProperty, int startIndex = 0)
        {
            if (keyArrayProperty.arraySize <= startIndex) yield break;
            var index = startIndex;
            var keyProperty = keyArrayProperty.GetArrayElementAtIndex(startIndex);
            var valueProperty = valueArrayProperty?.GetArrayElementAtIndex(startIndex);
            var endProperty = keyArrayProperty.GetEndProperty();

            do
            {
                yield return new EnumerationEntry(keyProperty, valueProperty, index);
                index++;
            } while (keyProperty.Next(false)
                     && (valueProperty == null || valueProperty.Next(false))
                     && !SerializedProperty.EqualContents(keyProperty, endProperty));
        }
    }

    [CustomPropertyDrawer(typeof(SerializableDictionaryBase.Storage), true)]
    public class SerializableDictionaryStoragePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            EditorGUI.PropertyField(position, property, label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            property.Next(true);
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}