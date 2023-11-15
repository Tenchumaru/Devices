namespace Lad {
	public static class Extensions {
		public static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> enumerable) {
			foreach (var item in enumerable) {
				hashSet.Add(item);
			}
		}

		public static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> enumerable) {
			foreach (var item in enumerable) {
				queue.Enqueue(item);
			}
		}
	}
}
