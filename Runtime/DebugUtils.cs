using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Hag.SerializableDictionary.Runtime
{
    public static class DebugUtils
    {
        public static string ToString(Array array)
        {
            if (array == null)
                return "null";
            return "{" + string.Join(", ", array.Cast<object>().Select(o => o.ToString()).ToArray()) + "}";
        }

        public static string ToString<TKey, TValue>(Dictionary<TKey, TValue> dict)
        {
            if (dict == null)
                return "null";
            return "{" + string.Join(", ",
                dict.Select(kvp => kvp.Key.ToString() + ":" + kvp.Value.ToString()).ToArray()) + "}";
        }
    }
}