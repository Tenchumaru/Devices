namespace Lad {
	internal static class Extensions {
		internal static void AddRange<T>(this HashSet<T> hashSet, IEnumerable<T> enumerable) {
			foreach (var item in enumerable) {
				hashSet.Add(item);
			}
		}

		internal static void EnqueueRange<T>(this Queue<T> queue, IEnumerable<T> enumerable) {
			foreach (var item in enumerable) {
				queue.Enqueue(item);
			}
		}
	}
}
