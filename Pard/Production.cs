namespace Pard {
	public class Production : NamedObject {
		public readonly Nonterminal Lhs;
		public readonly IReadOnlyList<Symbol> Rhs;
		public readonly int Index;
		public readonly ActionCode? ActionCode;
		public readonly Grammar.Associativity Associativity;
		public readonly int? Precedence;

		public Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int index, ActionCode? actionCode, Grammar.Associativity associativity, int? precedence) : this(lhs, rhs, index, actionCode) {
			Associativity = associativity;
			Precedence = precedence;
		}

		public Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int index, ActionCode? actionCode = null) : base(string.Format("{0} -> {1}", lhs, string.Join(" ", rhs))) {
			Lhs = lhs;
			Rhs = new List<Symbol>(rhs);
			Index = index;
			ActionCode = actionCode;
			var precedenceTerminal = (Terminal?)Rhs.LastOrDefault(s => s is Terminal);
			if (precedenceTerminal != null) {
				Associativity = precedenceTerminal.Associativity;
				Precedence = precedenceTerminal.Precedence;
			}
		}
	}
}
