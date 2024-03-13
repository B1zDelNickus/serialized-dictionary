﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hag.SerializableDictionary.Runtime
{
    public abstract class SerializableDictionaryBase
    {
        public abstract class Storage
        {
        }

        protected class Dictionary<TKey, TValue> : System.Collections.Generic.Dictionary<TKey, TValue>
        {
            public Dictionary()
            {
            }

            public Dictionary(IDictionary<TKey, TValue> dict) : base(dict)
            {
            }

            public Dictionary(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }

    [Serializable]
    public abstract class SerializableDictionaryBase<TKey, TValue, TValueStorage> : SerializableDictionaryBase,
        IDictionary<TKey, TValue>, IDictionary, ISerializationCallbackReceiver, IDeserializationCallback, ISerializable
    {
        private Dictionary<TKey, TValue> _dict;
        [SerializeField] private TKey[] keys;
        [SerializeField] private TValueStorage[] values;

        protected SerializableDictionaryBase()
        {
            _dict = new Dictionary<TKey, TValue>();
        }

        protected SerializableDictionaryBase(IDictionary<TKey, TValue> dict)
        {
            _dict = new Dictionary<TKey, TValue>(dict);
        }

        protected abstract void SetValue(TValueStorage[] storage, int i, TValue value);
        protected abstract TValue GetValue(TValueStorage[] storage, int i);

        public void CopyFrom(IDictionary<TKey, TValue> dict)
        {
            _dict.Clear();
            foreach (var kvp in dict)
            {
                _dict[kvp.Key] = kvp.Value;
            }
        }

        public void OnAfterDeserialize()
        {
            if (keys == null || values == null || keys.Length != values.Length) return;
            _dict.Clear();
            var n = keys.Length;
            for (var i = 0; i < n; ++i)
            {
                _dict[keys[i]] = GetValue(values, i);
            }

            keys = null;
            values = null;
        }

        public void OnBeforeSerialize()
        {
            var n = _dict.Count;
            keys = new TKey[n];
            values = new TValueStorage[n];

            var i = 0;
            foreach (var kvp in _dict)
            {
                keys[i] = kvp.Key;
                SetValue(values, i, kvp.Value);
                ++i;
            }
        }

        #region IDictionary<TKey, TValue>

        public ICollection<TKey> Keys => ((IDictionary<TKey, TValue>) _dict).Keys;

        public ICollection<TValue> Values => ((IDictionary<TKey, TValue>) _dict).Values;

        public int Count => ((IDictionary<TKey, TValue>) _dict).Count;

        public bool IsReadOnly => ((IDictionary<TKey, TValue>) _dict).IsReadOnly;

        public TValue this[TKey key]
        {
            get => ((IDictionary<TKey, TValue>) _dict)[key];
            set => ((IDictionary<TKey, TValue>) _dict)[key] = value;
        }

        public void Add(TKey key, TValue value)
        {
            ((IDictionary<TKey, TValue>) _dict).Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return ((IDictionary<TKey, TValue>) _dict).ContainsKey(key);
        }

        public bool Remove(TKey key)
        {
            return ((IDictionary<TKey, TValue>) _dict).Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return ((IDictionary<TKey, TValue>) _dict).TryGetValue(key, out value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            ((IDictionary<TKey, TValue>) _dict).Add(item);
        }

        public void Clear()
        {
            ((IDictionary<TKey, TValue>) _dict).Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>) _dict).Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((IDictionary<TKey, TValue>) _dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return ((IDictionary<TKey, TValue>) _dict).Remove(item);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>) _dict).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IDictionary<TKey, TValue>) _dict).GetEnumerator();
        }

        #endregion

        #region IDictionary

        public bool IsFixedSize => ((IDictionary) _dict).IsFixedSize;

        ICollection IDictionary.Keys => ((IDictionary) _dict).Keys;

        ICollection IDictionary.Values => ((IDictionary) _dict).Values;

        public bool IsSynchronized => ((IDictionary) _dict).IsSynchronized;

        public object SyncRoot => ((IDictionary) _dict).SyncRoot;

        public object this[object key]
        {
            get => ((IDictionary) _dict)[key];
            set => ((IDictionary) _dict)[key] = value;
        }

        public void Add(object key, object value)
        {
            ((IDictionary) _dict).Add(key, value);
        }

        public bool Contains(object key)
        {
            return ((IDictionary) _dict).Contains(key);
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return ((IDictionary) _dict).GetEnumerator();
        }

        public void Remove(object key)
        {
            ((IDictionary) _dict).Remove(key);
        }

        public void CopyTo(Array array, int index)
        {
            ((IDictionary) _dict).CopyTo(array, index);
        }

        #endregion

        #region IDeserializationCallback

        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback) _dict).OnDeserialization(sender);
        }

        #endregion

        #region ISerializable

        protected SerializableDictionaryBase(SerializationInfo info, StreamingContext context)
        {
            _dict = new Dictionary<TKey, TValue>(info, context);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable) _dict).GetObjectData(info, context);
        }

        #endregion
    }

    public static class SerializableDictionary
    {
        public abstract class Storage<T> : SerializableDictionaryBase.Storage
        {
            public T data;
        }
    }

    [Serializable]
    public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, TValue>
    {
        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict)
        {
        }

        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected override TValue GetValue(TValue[] storage, int i)
        {
            return storage[i];
        }

        protected override void SetValue(TValue[] storage, int i, TValue value)
        {
            storage[i] = value;
        }
    }

    [Serializable]
    public class
        SerializableDictionary<TKey, TValue, TValueStorage> : SerializableDictionaryBase<TKey, TValue, TValueStorage>
        where TValueStorage : SerializableDictionary.Storage<TValue>, new()
    {
        public SerializableDictionary()
        {
        }

        public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict)
        {
        }

        protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected override TValue GetValue(TValueStorage[] storage, int i)
        {
            return storage[i].data;
        }

        protected override void SetValue(TValueStorage[] storage, int i, TValue value)
        {
            storage[i] = new TValueStorage
            {
                data = value
            };
        }
    }
}