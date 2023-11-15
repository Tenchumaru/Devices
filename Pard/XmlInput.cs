using System.Xml.Linq;

namespace Pard {
	class XmlInput : IGrammarInput {
		private static readonly List<string> associativityNames = Enum.GetNames(typeof(Grammar.Associativity)).Select(s => s.ToLowerInvariant()).ToList();
		private readonly Options options;

		public XmlInput(Options options) => this.options = options;

		public (Nonterminal, IEnumerable<(string, int)>, IReadOnlyList<Production>, IReadOnlyList<ActionCode>) Read(TextReader reader) {
			XElement xml = XDocument.Load(reader).Element("grammar") ?? throw new ApplicationException("no grammar in file");
			var defines = xml.Elements("define").Select(u => (string?)u.Attribute("value") ?? throw new ApplicationException("no value for define"));
			options.DefineDirectives.AddRange(defines);
			var usings = xml.Elements("using").Select(u => (string?)u.Attribute("value") ?? throw new ApplicationException("no value for using"));
			options.AdditionalUsingDirectives.AddRange(usings);
			List<Production> productions = new();
			Dictionary<string, Terminal> terminals = new();
			Dictionary<string, Nonterminal> nonterminals = new();
			var symbols = xml.Elements("symbols").SelectMany(e => e.Elements()).Select((x, i) => new { Element = x, Precedence = i + 1 });
			foreach (var pair in symbols) {
				XElement symbol = pair.Element;
				var name = (string?)symbol.Attribute("name");
				var value = (string?)symbol.Attribute("value");
				var typeName = (string?)symbol.Attribute("type");
				switch (symbol.Name.LocalName) {
					case "literal":
						name = Terminal.FormatLiteralName(value);
						terminals.Add(name, new Terminal(name, typeName, GetAssociativity(symbol), pair.Precedence, name[1]));
						break;
					case "terminal":
						if (name == null) {
							throw new ApplicationException("no name for terminal");
						}
						terminals.Add(name, new Terminal(name, typeName, GetAssociativity(symbol), pair.Precedence));
						break;
					case "nonterminal":
						if (name == null) {
							throw new ApplicationException("no name for nonterminal");
						}
						nonterminals.Add(name, new Nonterminal(name, typeName));
						break;
					default:
						throw new ApplicationException($"unknown symbol element '{symbol.Name.LocalName}'");
				}
			}
			XElement rules = xml.Element("rules") ?? throw new ApplicationException("no rules in grammar");
			foreach (XElement rule in rules.Elements("rule")) {
				var name = (string)(rule.Attribute("name") ?? throw new ApplicationException("no name for rule"));
				Nonterminal lhs = nonterminals.TryGetValue(name, out Nonterminal? nonterminal) ? nonterminal : new Nonterminal(name, null);
				var q = from x in rule.Elements()
								where x.Name != "action"
								let v = (string?)x.Attribute("value")
								let n = (string?)x.Attribute("name")
								let l = x.Name == "literal" ? Terminal.FormatLiteralName(v) : null
								let s = l != null ? terminals.GetOrPutLiteral(l) :
									x.Name == "nonterminal" ? nonterminals.TryGetValue(n, out nonterminal) ? (Symbol)nonterminal : new Nonterminal(n, null) :
									x.Name == "terminal" ? terminals.GetOrPutNonliteral(n) :
									throw new ApplicationException($"unknown symbol element '{x.Name}'")
								select s;
				List<Symbol> rhs = q.ToList();
				var action = (string?)rule.Element("action");
				ActionCode? actionCode = action != null ? new ActionCode(action, 0) : null;
				Production production;
				var precedenceTokenName = (string?)rule.Attribute("precedence");
				if (precedenceTokenName != null) {
					production = terminals.TryGetValue(precedenceTokenName, out Terminal? precedenceToken) ?
						new Production(lhs, rhs, productions.Count, actionCode, precedenceToken.Associativity, precedenceToken.Precedence) :
						throw new ApplicationException($"precedence token '{precedenceTokenName}' not found");
				} else {
					production = new Production(lhs, rhs, productions.Count, actionCode);
				}
				productions.Add(production);
			}
			return (productions[0].Lhs, terminals.Values.Select((t) => (t.Name, t.Value)), productions, Array.Empty<ActionCode>());
		}

		private static Grammar.Associativity GetAssociativity(XElement symbol) {
			string associativityString = (string?)symbol.Attribute("associativity") ?? Grammar.Associativity.None.ToString();
			Grammar.Associativity associativity = associativityNames.Contains(associativityString.ToLowerInvariant()) ?
				Enum.Parse<Grammar.Associativity>(associativityString, true) :
				throw new ApplicationException($"unknown associativity '{associativityString}'");
			return associativity;
		}
	}

	public static class Extensions {
		private static Terminal GetOrPut(this Dictionary<string, Terminal> dict, string name, Func<Terminal> fn) {
			if (dict.TryGetValue(name, out Terminal? terminal)) {
				return terminal;
			} else {
				terminal = fn();
			}
			dict.Add(name, terminal);
			return terminal;
		}

		public static Terminal GetOrPutLiteral(this Dictionary<string, Terminal> dict, string name) {
			return GetOrPut(dict, name, () => new Terminal(name, null, Grammar.Associativity.None, 0, name[1]));
		}

		public static Terminal GetOrPutNonliteral(this Dictionary<string, Terminal> dict, string name) {
			return GetOrPut(dict, name, () => new Terminal(name, null, Grammar.Associativity.None, 0));
		}
	}
}
