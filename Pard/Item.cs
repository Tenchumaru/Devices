using System.Text;

namespace Pard {
	// LR(1) item, p. 230
	class Item {
		public readonly int ProductionIndex;
		public readonly int DotPosition;
		public readonly Terminal Lookahead;
		private readonly string name = "";

		public Item(int productionIndex, int dotPosition, Terminal lookahead) {
			ProductionIndex = productionIndex;
			DotPosition = dotPosition;
			Lookahead = lookahead;
			name = string.Format("{0} @ {1}, {2}", productionIndex, dotPosition, lookahead.Name);
		}

		public override bool Equals(object? obj) => obj is Item that && ToString() == that.ToString();

		public override int GetHashCode() => name.GetHashCode();

		public override string ToString() => name;

		public static bool operator ==(Item? left, Item? right) => left is null ? right is null : left.Equals(right);

		public static bool operator !=(Item? left, Item? right) => !(left == right);

		internal class Set {
			public int Count => set.Count;
			public readonly Dictionary<Symbol, Set> Gotos = new();

			private readonly string name = "";
			private readonly HashSet<Item> set;

			public override bool Equals(object? obj) => obj is Set that && ToString() == that.ToString();

			public override int GetHashCode() => name.GetHashCode();

			public override string ToString() => name;

			public static bool operator ==(Set? left, Set? right) => left is null ? right is null : left.Equals(right);

			public static bool operator !=(Set? left, Set? right) => !(left == right);

			internal Set(IEnumerable<Item> items) {
				set = new HashSet<Item>(items);
				if (set.Any()) {
					// Use kernel items (the first item, which has the augmented start symbol, and any item whose dot position is greater
					// than zero) for the name of this set.
					var q = from i in set
									where i.ProductionIndex < 0 || i.DotPosition > 0
									select i.name;
					name = string.Join("; ", q);
				}
			}

			internal bool Any() => set.Any();

			internal IEnumerable<Item> AsEnumerable() => set.AsEnumerable();

			internal void UnionWith(IEnumerable<Item> items) => set.UnionWith(items);

			internal string ToString(IDictionary<int, Production> productions) {
				StringBuilder sb = new();
				foreach (Item item in set) {
					if (item.ProductionIndex < 0) {
						sb.AppendFormat("start {0}", item.DotPosition == 0 ? "before" : "after");
					} else {
						Production production = productions[item.ProductionIndex];
						sb.AppendFormat("{0} ->", production.Lhs);
						var before = production.Rhs.Take(item.DotPosition);
						foreach (Symbol rhe in before) {
							sb.AppendFormat(" {0}", rhe);
						}
						sb.Append(" .");
						var after = production.Rhs.Skip(item.DotPosition);
						foreach (Symbol rhe in after) {
							sb.AppendFormat(" {0}", rhe);
						}
					}
					sb.AppendFormat(" [{0}]{1}", item.Lookahead, Environment.NewLine);
				}
				return sb.ToString();
			}
		}
	}
}
