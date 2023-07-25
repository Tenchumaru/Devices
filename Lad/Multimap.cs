using System.Collections;

namespace Lad {
	public class Multimap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull {
		public Dictionary<TKey, HashSet<TValue>>.KeyCollection Keys => dictionary.Keys;
		private readonly Dictionary<TKey, HashSet<TValue>> dictionary = new();
		private readonly List<KeyValuePair<TKey, TValue>> list = new();

		public void Add(TKey key, TValue value) {
			if (!dictionary.TryGetValue(key, out HashSet<TValue>? values)) {
				dictionary.Add(key, values = new HashSet<TValue>());
			}
			values.Add(value);
			list.Add(new KeyValuePair<TKey, TValue>(key, value));
		}

		public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> collection) {
			foreach (KeyValuePair<TKey, TValue> pair in collection) {
				Add(pair.Key, pair.Value);
			}
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => list.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

		public void Clear() {
			dictionary.Clear();
			list.Clear();
		}
	}
}
