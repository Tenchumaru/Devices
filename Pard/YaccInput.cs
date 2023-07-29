namespace Pard {
	public partial class YaccInput : IGrammarInput {
		private readonly List<Production> productions = new();
		private Grammar.Associativity terminalAssociativity;
		private string? terminalTypeName;
		private int? precedence;
		private readonly Options options;
		private readonly Dictionary<string, Terminal> knownTerminals = new();
		private readonly Dictionary<string, Nonterminal> knownNonterminals = new();

		public YaccInput(Options options) => this.options = options;

		public IReadOnlyList<Production> Read(TextReader reader) {
			precedence = 0;
			knownTerminals.Clear();
			knownNonterminals.Clear();
			YaccInputParser parser = new(this, new YaccInputScanner(reader));
			return parser.Parse() ? productions : throw new ApplicationException("syntax error");
		}

		private void CreateNonterminals(string? typeName, List<string> names) => names.ForEach(s => knownNonterminals[s] = new Nonterminal(s, typeName));

		private void SetTerminalParameters(Grammar.Associativity associativity, int? precedence, string? typeName) {
			terminalAssociativity = associativity;
			this.precedence = precedence;
			terminalTypeName = typeName;
		}

		private void AddTerminal(string name) {
			if (knownTerminals.ContainsKey(name)) {
				throw new ApplicationException($"Terminal '{name}' already declared");
			}
			knownTerminals.Add(name, new Terminal(name, terminalTypeName, terminalAssociativity, precedence));
		}

		private void AddLiteral(char ch) => knownTerminals.Add(Terminal.FormatLiteralName(ch), new Terminal(Terminal.FormatLiteralName(ch), terminalTypeName, terminalAssociativity, precedence, ch));

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
			if (!knownNonterminals.TryGetValue(ruleName, out Nonterminal? nonterminal)) {
				nonterminal = new Nonterminal(ruleName, null);
				knownNonterminals.Add(ruleName, nonterminal);
			}
			productions.Add(new Production(nonterminal, rhs, productions.Count, actionCode,
				terminal != null ? terminal.Associativity : Grammar.Associativity.None,
				terminal != null ? terminal.Precedence : 0));
		}

		private Terminal GetTerminal(string name) {
			if (!knownTerminals.TryGetValue(name, out Terminal? terminal)) {
				terminal = new Terminal(name, null, Grammar.Associativity.None, 0);
				knownTerminals.Add(name, terminal);
			}
			return terminal;
		}

		private Symbol GetSymbol(string name) {
			if (knownTerminals.TryGetValue(name, out Terminal? terminal)) {
				return terminal;
			} else if (knownNonterminals.TryGetValue(name, out Nonterminal? nonterminal)) {
				return nonterminal;
			} else {
				nonterminal = new Nonterminal(name, null);
				knownNonterminals.Add(name, nonterminal);
				return nonterminal;
			}
		}

		private Terminal GetLiteral(char ch) {
			string name = Terminal.FormatLiteralName(ch);
			if (!knownTerminals.TryGetValue(name, out Terminal? terminal)) {
				terminal = new Terminal(name, null, Grammar.Associativity.None, 0, ch);
				knownTerminals.Add(name, terminal);
			}
			return terminal;
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
