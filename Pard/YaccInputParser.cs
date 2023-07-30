namespace Pard {
	public partial class YaccInput {
		public partial class YaccInputParser {
			private readonly List<ActionCode> sectionOneCodeBlocks = new();
			private ActionCode? sectionThreeCodeBlock;
			private string? startRuleName;
			private readonly YaccInput yaccInput;

			public YaccInputParser(YaccInput yaccInput, YaccInputScanner scanner) : this(scanner) => this.yaccInput = yaccInput;

			private void AddTypedNonterminals(string? typeName, List<string> names) {
				yaccInput.CreateNonterminals(typeName, names);
			}

			private void AddTokens(Grammar.Associativity terminalAssociativity, string? terminalTypeName, List<IConvertible> names) {
				yaccInput.SetTerminalParameters(terminalAssociativity, terminalTypeName);
				foreach (var name in names) {
					if (name is char ch) {
						yaccInput.AddLiteral(ch);
					} else {
						yaccInput.AddTerminal((string)name);
					}
				}
			}

			private void AddRules(string ruleName, List<KeyValuePair<List<Symbol>, Terminal?>> rhss) {
				foreach (var rhs in rhss) {
					yaccInput.AddProduction(ruleName, rhs.Key, rhs.Value);
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
	}
}
