using System.Xml.Linq;

namespace Pard {
	class XmlInput : IGrammarInput {
		private static readonly List<string> associativityNames = Enum.GetNames(typeof(Grammar.Associativity)).Select(s => s.ToLowerInvariant()).ToList();
		private Options options;

		public XmlInput(Options options) => this.options = options;

		public IReadOnlyList<Production> Read(TextReader reader) {
			XElement xml = XDocument.Load(reader).Element("grammar") ?? throw new ApplicationException("no grammar in file");
			var defines = xml.Elements("define").Select(u => (string)(u.Attribute("value") ?? throw new ApplicationException("no value for define")));
			options.DefineDirectives.AddRange(defines);
			var usings = xml.Elements("using").Select(u => (string)(u.Attribute("value") ?? throw new ApplicationException("no value for using")));
			options.AdditionalUsingDirectives.AddRange(usings);
			List<Production> productions = new();
			HashSet<Terminal> knownTerminals = new();
			HashSet<Nonterminal> knownNonterminals = new();
			var symbols = xml.Elements("symbols").SelectMany(e => e.Elements()).Select((x, i) => new { Element = x, Precedence = i + 1 });
			foreach (var pair in symbols) {
				XElement symbol = pair.Element;
				var name = (string?)symbol.Attribute("name");
				var value = (string?)symbol.Attribute("value");
				var typeName = (string?)symbol.Attribute("type");
				switch (symbol.Name.LocalName) {
					case "literal":
						name = Terminal.FormatLiteralName(value);
						knownTerminals.Add(new Terminal(name, typeName, GetAssociativity(symbol), pair.Precedence, name[1]));
						break;
					case "terminal":
						if (name == null) {
							throw new ApplicationException("no name for terminal");
						}
						knownTerminals.Add(new Terminal(name, typeName, GetAssociativity(symbol), pair.Precedence));
						break;
					case "nonterminal":
						if (name == null) {
							throw new ApplicationException("no name for nonterminal");
						}
						knownNonterminals.Add(new Nonterminal(name, typeName));
						break;
					default:
						throw new ApplicationException($"unknown symbol element '{symbol.Name.LocalName}'");
				}
			}
			Dictionary<string, Terminal> terminals = knownTerminals.ToDictionary(t => t.Name);
			Dictionary<string, Nonterminal> nonterminals = knownNonterminals.ToDictionary(t => t.Name);
			XElement rules = xml.Element("rules") ?? throw new ApplicationException("no rules in grammar");
			foreach (XElement rule in rules.Elements("rule")) {
				var name = (string)(rule.Attribute("name") ?? throw new ApplicationException("no name for rule"));
				Nonterminal lhs = nonterminals.TryGetValue(name, out Nonterminal? nonterminal) ? nonterminal : new Nonterminal(name, null);
				var q = from x in rule.Elements()
								where x.Name != "action"
								let v = (string?)x.Attribute("value")
								let n = (string?)x.Attribute("name")
								let l = x.Name == "literal" ? Terminal.FormatLiteralName(v) : null
								let s = l != null ? terminals.TryGetValue(l, out Terminal? terminal) ? terminal : new Terminal(l, null, Grammar.Associativity.None, 0, l[1]) :
									x.Name == "nonterminal" ? nonterminals.TryGetValue(n, out nonterminal) ? (Symbol)nonterminal : new Nonterminal(n, null) :
									x.Name == "terminal" ? terminals.TryGetValue(n, out terminal) ? terminal : new Terminal(n, null, Grammar.Associativity.None, 0) :
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
			return productions;
		}

		private static Grammar.Associativity GetAssociativity(XElement symbol) {
			string associativityString = (string?)symbol.Attribute("associativity") ?? Grammar.Associativity.None.ToString();
			Grammar.Associativity associativity = associativityNames.Contains(associativityString.ToLowerInvariant()) ?
				Enum.Parse<Grammar.Associativity>(associativityString, true) :
				throw new ApplicationException($"unknown associativity '{associativityString}'");
			return associativity;
		}
	}
}
