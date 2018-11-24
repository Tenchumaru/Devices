using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    public class Multimap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private Dictionary<TKey, HashSet<TValue>> dictionary = new Dictionary<TKey, HashSet<TValue>>();
        private List<KeyValuePair<TKey, TValue>> list = new List<KeyValuePair<TKey, TValue>>();

        public void Add(TKey key, TValue value)
        {
            if(!dictionary.TryGetValue(key, out HashSet<TValue> values))
                dictionary.Add(key, values = new HashSet<TValue>());
            values.Add(value);
            list.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach(var pair in collection)
                Add(pair.Key, pair.Value);
        }

        /// <summary>
        /// Finds all elements that have the provided key.
        /// </summary>
        public List<TValue> FindAll(TKey key)
        {
            return dictionary.TryGetValue(key, out HashSet<TValue> values) ? new List<TValue>(values) : new List<TValue>();
        }

        public Dictionary<TKey, HashSet<TValue>>.KeyCollection Keys
        {
            get { return dictionary.Keys; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }
    }
}
