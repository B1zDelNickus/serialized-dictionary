using System.Text;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Hag.SerializableDictionary.Editor
{
    public static class DebugUtilsEditor
    {
        public static string ToString(SerializedProperty property)
        {
            var sb = new StringBuilder();
            var iterator = property.Copy();
            var end = property.GetEndProperty();
            do
            {
                sb.AppendLine(iterator.propertyPath + " (" + iterator.type + " " + iterator.propertyType + ") = "
                              + SerializableDictionaryPropertyDrawer.GetPropertyValue(iterator)
#if UNITY_5_6_OR_NEWER
                              + (iterator.isArray ? " (" + iterator.arrayElementType + ")" : "")
#endif
                );
            } while (iterator.Next(true) && iterator.propertyPath != end.propertyPath);

            return sb.ToString();
        }
    }
}