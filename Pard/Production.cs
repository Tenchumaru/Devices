namespace Pard {
	public class Production : NamedObject {
		public readonly Nonterminal Lhs;
		public readonly IReadOnlyList<Symbol> Rhs;
		public readonly int Index;
		public readonly ActionCode? ActionCode;
		public readonly Grammar.Associativity Associativity;
		public readonly int? Precedence;
		public readonly int LineNumber;

		public Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int index, ActionCode? actionCode, Grammar.Associativity associativity, int? precedence, int lineNumber = 0) : this(lhs, rhs, index, actionCode, lineNumber) {
			Associativity = associativity;
			Precedence = precedence;
		}

		public Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int index, ActionCode? actionCode = null, int lineNumber = 0) : base($"{lhs} -> {string.Join(" ", rhs)}") {
			Lhs = lhs;
			Rhs = new List<Symbol>(rhs);
			Index = index;
			ActionCode = actionCode;
			LineNumber = lineNumber;
			var precedenceTerminal = (Terminal?)Rhs.LastOrDefault(s => s is Terminal);
			if (precedenceTerminal != null) {
				Associativity = precedenceTerminal.Associativity;
				Precedence = precedenceTerminal.Precedence;
			}
		}
	}
}
