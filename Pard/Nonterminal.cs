namespace Pard {
	public class Nonterminal : Symbol {
		public static readonly Nonterminal AugmentedStart = new Nonterminal("(start)", null);

		public Nonterminal(string name, string? typeName) : base(name, typeName) {
		}
	}
}
