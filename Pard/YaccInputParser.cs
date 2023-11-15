namespace Pard {
	public partial class YaccInput {
		public partial class YaccInputParser {
			public string? StartingRuleName { get; private set; }
			private readonly YaccInput yaccInput;

			public YaccInputParser(YaccInput yaccInput, YaccInputScanner scanner) : this(scanner) => this.yaccInput = yaccInput;

			private void AddTypedNonterminals(string? typeName, List<string> names) {
				yaccInput.CreateNonterminals(typeName, names);
			}

			private void AddTokens(TokenDefinition tokenDefinition, List<IConvertible> names) {
				yaccInput.SetTerminalParameters(tokenDefinition.Associativity, tokenDefinition.Precedence, tokenDefinition.TerminalTypeName);
				foreach (var name in names) {
					if (name is char ch) {
						yaccInput.AddLiteral(ch);
					} else {
						yaccInput.AddTerminal((string)name);
					}
				}
			}

			private void AddProductions(string ruleName, List<Rhs> rhss) {
				foreach (var rhs in rhss) {
					yaccInput.AddProduction(ruleName, rhs.Symbols, rhs.PrecedenceTerminal, scanner.LineNumber);
				}
			}

			private Symbol GetLiteral(char ch) {
				return yaccInput.GetLiteral(ch);
			}

			private Symbol GetSymbol(string name) {
				return yaccInput.GetSymbol(name);
			}

			private Terminal GetTerminal(string name) {
				return yaccInput.GetTerminal(name);
			}
		}

		public class Rhs {
			public List<Symbol> Symbols { get; } = new List<Symbol>();
			public Terminal? PrecedenceTerminal { get; set; }
		}

		public class TokenDefinition {
			public Grammar.Associativity Associativity { get; }
			public int? Precedence { get; }
			public string? TerminalTypeName { get; }

			public TokenDefinition(KeyValuePair<Grammar.Associativity, int?> panda, string? terminalTypeName) {
				Associativity = panda.Key;
				Precedence = panda.Value;
				TerminalTypeName = terminalTypeName;
			}
		}
	}
}
