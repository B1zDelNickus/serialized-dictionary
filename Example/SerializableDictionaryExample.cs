using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Hag.SerializableDictionary.Example
{
	public class SerializableDictionaryExample : MonoBehaviour {
		// The dictionaries can be accessed throught a property
		[SerializeField] private StringStringDictionary mStringStringDictionary;
		
		public IDictionary<string, string> StringStringDictionary
		{
			set => mStringStringDictionary.CopyFrom (value);
		}

		public ObjectColorDictionary objectColorDictionary;
		public StringColorArrayDictionary stringColorArrayDictionary;
#if NET_4_6 || NET_STANDARD_2_0
		public StringHashSet stringHashSet;
#endif

		void Start ()
		{
			// access by property
			StringStringDictionary = new Dictionary<string, string> { {"first key", "value A"}, {"second key", "value B"}, {"third key", "value C"} };
			objectColorDictionary = new ObjectColorDictionary { {gameObject, Color.blue}, {this, Color.red} };
		}
	}
}
