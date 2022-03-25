using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MadMaps.Common.Collections
{
    [Serializable]
    public class CompositionDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField, HideInInspector]
        private List<TKey> _keys = new List<TKey>();
        [SerializeField, HideInInspector]
        private List<TValue> _values = new List<TValue>();

        protected Dictionary<TKey, TValue> WorkingDictionary = new Dictionary<TKey, TValue>();

        public List<TKey> GetKeys()
        {
            return WorkingDictionary.Keys.ToList();
        }

        public List<TValue> GetValues()
        {
            return WorkingDictionary.Values.ToList();
        }

        public virtual void Add(TKey key, TValue value)
        {
            WorkingDictionary.Add(key, value);
        }

        public virtual bool Remove(TKey key)
        {
            return WorkingDictionary.Remove(key);
        }

        public virtual int Count
        {
            get { return WorkingDictionary.Count; }
        }

        public virtual bool ContainsKey(TKey key)
        {
            return WorkingDictionary.ContainsKey(key);
        }

        public virtual bool ContainsValue(TValue value)
        {
            return WorkingDictionary.ContainsValue(value);
        }

        public virtual bool TryGetValue(TKey key, out TValue result)
        {
            return WorkingDictionary.TryGetValue(key, out result);
        }

        public virtual Dictionary<TKey, TValue>.Enumerator GetEnumerator()
        {
            return WorkingDictionary.GetEnumerator();
        }

        public virtual KeyValuePair<TKey, TValue> First()
        {
            return WorkingDictionary.First();
        }

        public TValue this[TKey key]
        {
            get { return WorkingDictionary[key]; }
            set { WorkingDictionary[key] = value; }
        }

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            _keys.Clear();
            _values.Clear();
            foreach (var kvp in WorkingDictionary)
            {
                if (kvp.Key == null)
                {
                    Debug.LogError("Failed to serialize null key! Value was " + kvp.Value);
                    continue;
                }
                _keys.Add(kvp.Key);
                _values.Add(kvp.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            WorkingDictionary.Clear();
            for (int i = 0; i < Mathf.Min(_keys.Count, _values.Count); i++)
            {
                if (_keys[i] == null)
                {
                    Debug.LogWarning("Failed to deserialize element for CompositionDictionary - key was null! Value was: " + _values[i]);
                    continue;
                }
                WorkingDictionary.Add(_keys[i], _values[i]);
            }
            _keys.Clear();
            _values.Clear();
        }

        public virtual void Clear()
        {
            WorkingDictionary.Clear();
            _keys.Clear();
            _values.Clear();
        }


    }
}