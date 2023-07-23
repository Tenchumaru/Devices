namespace Pard {
	public partial class YaccInput : IGrammarInput {
		private readonly List<Production> productions = new();
		private Grammar.Associativity terminalAssociativity;
		private string? terminalTypeName;
		private int precedence;
		private readonly Options options;
		private readonly HashSet<Terminal> knownTerminals = new();
		private readonly HashSet<Nonterminal> knownNonterminals = new();

		public YaccInput(Options options) => this.options = options;

		public IReadOnlyList<Production> Read(TextReader reader) {
			precedence = 0;
			knownTerminals.Clear();
			knownNonterminals.Clear();
			YaccInputParser parser = new(this, new YaccInputScanner(reader));
			return parser.Parse() ? productions : throw new InvalidOperationException("syntax error");
		}

		private void CreateNonterminals(string? typeName, List<string> names) => knownNonterminals.UnionWith(names.Select(s => new Nonterminal(s, typeName)));

		private void SetTerminalParameters(Grammar.Associativity associativity, string? typeName) {
			terminalAssociativity = associativity;
			terminalTypeName = typeName;
		}

		private void AddTerminal(string name) => knownTerminals.Add(new Terminal(name, terminalTypeName, terminalAssociativity, precedence));

		private void AddLiteral(char ch) => knownTerminals.Add(new Terminal("'" + ch + "'", terminalTypeName, terminalAssociativity, precedence, ch));

		private void AddProduction(string ruleName, List<Symbol> rhs, Terminal? terminal) {
			// Replace code blocks with synthesized non-terminals for rules
			// using those code blocks.
			for (int i = 0, count = rhs.Count - 1; i < count; ++i) {
				var innerCodeBlock = rhs[i] as CodeBlockSymbol;
				if (innerCodeBlock != null) {
					string subruleName = string.Format("{0}.{1}", ruleName, i + 1);
					var subruleSymbol = new Nonterminal(subruleName, null);
					var subruleProduction = new Production(subruleSymbol, Array.Empty<Symbol>(), productions.Count, innerCodeBlock.ActionCode);
					productions.Add(subruleProduction);
					rhs[i] = subruleSymbol;
				}
			}
			var lastCodeBlock = rhs.LastOrDefault() as CodeBlockSymbol;
			ActionCode? actionCode = null;
			if (lastCodeBlock != null) {
				rhs.RemoveAt(rhs.Count - 1);
				actionCode = lastCodeBlock.ActionCode;
			}
			var nonterminal = new Nonterminal(ruleName, null);
			if (!knownNonterminals.Add(nonterminal))
				nonterminal = knownNonterminals.First(n => n == nonterminal);
				productions.Add(new Production(nonterminal, rhs, productions.Count, actionCode,
					terminal != null ? terminal.Associativity : Grammar.Associativity.None,
					terminal != null ? terminal.Precedence : 0));
		}

		private Terminal GetTerminal(string name) {
			var terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
			if (!knownTerminals.Add(terminal))
				terminal = knownTerminals.First(t => t == terminal);
			return terminal;
		}

		private Symbol GetSymbol(string name) {
			var terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
			Symbol? symbol = knownTerminals.FirstOrDefault(t => t == terminal);
			if (symbol == null) {
				var nonterminal = new Nonterminal(name, null);
				symbol = knownNonterminals.Add(nonterminal) ? nonterminal : knownNonterminals.First(n => n == nonterminal);
			}
			return symbol;
		}

		private static Symbol GetLiteral(char ch) {
			// TODO:  escape character.
			return new Terminal("'" + ch + "'", null, Grammar.Associativity.None, 0, ch);
		}

		private class CodeBlockSymbol : Symbol {
			internal readonly ActionCode ActionCode;

			internal CodeBlockSymbol(ActionCode actionCode) : base(Guid.NewGuid().ToString(), null) {
				ActionCode = actionCode;
			}
		}

		public class Token {
			public int Symbol;
			public object? Value;
			public static readonly Token End = new() { Symbol = -1 };
		}

		private enum InputState { Section1, Section2Declaration, Section2Definition }
	}
}
