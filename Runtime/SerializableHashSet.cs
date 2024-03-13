#if NET_4_6 || NET_STANDARD_2_0
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hag.SerializableDictionary.Runtime
{
    public abstract class SerializableHashSetBase
    {
        public abstract class Storage
        {
        }

        protected class HashSet<TValue> : System.Collections.Generic.HashSet<TValue>
        {
            public HashSet()
            {
            }

            public HashSet(ISet<TValue> set) : base(set)
            {
            }

            public HashSet(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }

    [Serializable]
    public abstract class SerializableHashSet<T> : SerializableHashSetBase, ISet<T>, ISerializationCallbackReceiver,
        IDeserializationCallback, ISerializable
    {
        private HashSet<T> _hashSet;
        [SerializeField] private T[] keys;

        protected SerializableHashSet()
        {
            _hashSet = new HashSet<T>();
        }

        protected SerializableHashSet(ISet<T> set)
        {
            _hashSet = new HashSet<T>(set);
        }

        // ReSharper disable once ParameterTypeCanBeEnumerable.Global
        public void CopyFrom(ISet<T> set)
        {
            _hashSet.Clear();
            foreach (var value in set)
            {
                _hashSet.Add(value);
            }
        }

        public void OnAfterDeserialize()
        {
            if (keys == null) return;
            _hashSet.Clear();
            var n = keys.Length;
            for (var i = 0; i < n; ++i)
            {
                _hashSet.Add(keys[i]);
            }

            keys = null;
        }

        public void OnBeforeSerialize()
        {
            var n = _hashSet.Count;
            keys = new T[n];

            var i = 0;
            foreach (var value in _hashSet)
            {
                keys[i] = value;
                ++i;
            }
        }

        #region ISet<TValue>

        public int Count => ((ISet<T>) _hashSet).Count;

        public bool IsReadOnly => ((ISet<T>) _hashSet).IsReadOnly;

        public bool Add(T item)
        {
            return ((ISet<T>) _hashSet).Add(item);
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            ((ISet<T>) _hashSet).ExceptWith(other);
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            ((ISet<T>) _hashSet).IntersectWith(other);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return ((ISet<T>) _hashSet).IsProperSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return ((ISet<T>) _hashSet).IsProperSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            return ((ISet<T>) _hashSet).IsSubsetOf(other);
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return ((ISet<T>) _hashSet).IsSupersetOf(other);
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            return ((ISet<T>) _hashSet).Overlaps(other);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return ((ISet<T>) _hashSet).SetEquals(other);
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            ((ISet<T>) _hashSet).SymmetricExceptWith(other);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            ((ISet<T>) _hashSet).UnionWith(other);
        }

        void ICollection<T>.Add(T item)
        {
            if (item != null) ((ISet<T>) _hashSet).Add(item);
        }

        public void Clear()
        {
            ((ISet<T>) _hashSet).Clear();
        }

        public bool Contains(T item)
        {
            return ((ISet<T>) _hashSet).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((ISet<T>) _hashSet).CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return ((ISet<T>) _hashSet).Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((ISet<T>) _hashSet).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((ISet<T>) _hashSet).GetEnumerator();
        }

        #endregion

        #region IDeserializationCallback

        public void OnDeserialization(object sender)
        {
            ((IDeserializationCallback) _hashSet).OnDeserialization(sender);
        }

        #endregion

        #region ISerializable

        protected SerializableHashSet(SerializationInfo info, StreamingContext context)
        {
            _hashSet = new HashSet<T>(info, context);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ((ISerializable) _hashSet).GetObjectData(info, context);
        }

        #endregion
    }
}
#endif