using System.Text;
using System.Text.RegularExpressions;

namespace Lad {
	public class LexGenerator : GeneratorBase, IGenerator {
		private readonly Regex continuationRx = new(@"\s*\|");
		private readonly Regex namedExpressionRx = new(@"^([A-Za-z]\w+)\s+(.+)");
		private readonly Regex startStateRx = new(@"<([A-Z_a-z][0-9A-Z_a-z]*)>{");
		private readonly StringBuilder sectionOneCode = new();
		private readonly StringBuilder moreCode = new();
		private readonly List<string> defineDirectives;
		private readonly List<string> additionalUsingDirectives;
		private readonly string[] namespaceNames;
		private readonly string[] classAccesses;
		private readonly string[] classNames;
		private string[] startStateNames = Array.Empty<string>();

		public LexGenerator(Options options) : base(options) {
			defineDirectives = options.DefineDirectives;
			additionalUsingDirectives = options.AdditionalUsingDirectives;
			namespaceNames = options.NamespaceNames;
			classAccesses = options.ClassAccesses;
			classNames = options.ClassNames;
		}

		protected override IEnumerable<StateMachine>? ProcessInput(string text) {
			string[] lines = text.Split('\n').Select(s => s.TrimEnd()).ToArray();
			string startState = "INITIAL";
			bool foundError = false;
			Dictionary<Nfa, int> rules = new();
			List<StringBuilder> codes = new();
			Dictionary<string, (Dictionary<Nfa, int>, List<StringBuilder>)> startStates = new();
			State state = State.InSectionOne;
			foreach ((string line, int lineNumber) in lines.Select((l, i) => (l, i + 1))) {
				switch (state) {
					case State.InSectionOne:
						if (line == "%{") {
							state = State.InSectionOneCode;
						} else if (line == "%%") {
							state = State.InSectionTwo;
						} else if (line.StartsWith("%option")) {
							// There are no options for now.
						} else if (line == "" || line[0] == '/' || line.StartsWith("%x")) {
							// Ignore blank lines, comments, remarks, and start state declarations.
						} else if (char.IsLetter(line[0])) {
							foundError |= !MakeNamedExpression(line, namedExpressionRx);
						} else {
							Console.Error.WriteLine($"syntax error in line {lineNumber}");
							foundError = true;
						}
						break;
					case State.InSectionOneCode:
						if (line == "%}") {
							state = State.InSectionOne;
						} else {
							sectionOneCode.AppendLine(line);
						}
						break;
					case State.InSectionTwo:
						if (line == "") {
							// Ignore.
						} else if (line[0] == '<') {
							// Check for start state.
							Match match = startStateRx.Match(line);
							if (match.Success) {
								startStates[startState] = (rules, codes);
								rules = new Dictionary<Nfa, int>();
								codes = new List<StringBuilder>();
								startState = match.Groups[1].Value;
							} else {
								Console.Error.WriteLine($"invalid start state expression in line {lineNumber}");
								foundError = true;
							}
						} else if (line == "}") {
							startStates[startState] = (rules, codes);
							startState = "INITIAL";
							(rules, codes) = startStates[startState];
						} else if (line[0] == '"') {
							string? s = StartWithLiteral(line, rules, codes);
							if (s != null) {
								Console.Error.WriteLine($"{s} in line {lineNumber}");
								foundError = true;
							}
						} else if (line == "%%") {
							if (startState != "INITIAL") {
								Console.Error.WriteLine($"unclosed start state in line {lineNumber}");
								foundError = true;
							}
							state = State.InSectionThree;
						} else if (char.IsWhiteSpace(line[0])) {
							if (codes.Any()) {
								codes.Last().AppendLine(line);
							}
						} else {
							string? s = StartWithRegularExpression(line, rules, codes);
							if (s != null) {
								Console.Error.WriteLine($"{s} in line {lineNumber}");
								foundError = true;
							}
						}
						break;
					case State.InSectionThree:
						moreCode.AppendLine(line);
						break;
				}
			}
			if (state == State.InSectionTwo && startState != "INITIAL") {
				Console.Error.WriteLine($"unclosed start state");
				foundError = true;
			}
			startStateNames = startStates.Keys.OrderBy((s) => s != "INITIAL").ToArray();
			var q = from p in startStates
							let r = p.Value.Item1
							let c = p.Value.Item2
							select new StateMachine($"internal Token? Read{p.Key}()", null, null, r, c.Select(s => s.ToString()), null);
			return foundError ? default : q.ToArray();
		}

		protected override void WriteActions(int index, StateMachine stateMachine, StringWriter writer) {
			writer.WriteLine("switch(longest.Key){");
			writer.WriteLine("#pragma warning disable CS0162 // Unreachable code detected");
			foreach ((string code, int ruleIndex) in stateMachine.Codes.Select((s, i) => (s, i))) {
				writer.WriteLine($"case {ruleIndex + 1}:");
				if (ruleIndex == stateMachine.DefaultActionIndex) {
					writer.WriteLine("default:");
				}
				writer.WriteLine(code);
				writer.WriteLine("break;");
			}
			writer.WriteLine("#pragma warning restore CS0162 // Unreachable code detected");
			writer.WriteLine('}');
		}

		protected override void WriteFooter(StringWriter writer) {
			foreach ((string s, int i) in startStateNames.Select((s, i) => (s, i))) {
				writer.WriteLine($"private const int {s}={i};");
			}
			writer.WriteLine("private int startState_=INITIAL;");
			writer.WriteLine("public Token? Read(){");
			writer.WriteLine("switch(startState_){");
			foreach(string s in startStateNames) {
				writer.WriteLine($"case {s}:");
				writer.WriteLine($"return Read{s}();");
			}
			writer.WriteLine('}');
			writer.WriteLine("throw new InvalidOperationException($\"invalid start state {startState_}\");");
			writer.WriteLine('}');
			writer.WriteLine("private void BEGIN(int startState){");
			writer.WriteLine("startState_=startState;");
			writer.WriteLine('}');
			writer.Write(moreCode.ToString());
			writer.WriteLine(bones[2]);
			writer.WriteLine(new string('}', namespaceNames.Length + classNames.Length - 1));
		}

		protected override void WriteHeader(StringWriter writer) {
			foreach (var directive in defineDirectives) {
				writer.WriteLine(directive);
			}
			writer.Write(sectionOneCode.ToString());
			foreach (var directive in additionalUsingDirectives) {
				writer.WriteLine(directive);
			}
			writer.Write(bones[0]);
			foreach (string namespaceName in namespaceNames) {
				writer.Write("namespace ");
				writer.Write(namespaceName);
				writer.Write('{');
			}
			var classPairs = classAccesses.Zip(classNames);
			foreach ((string classAccess, string className) in classPairs) {
				writer.Write(classAccess);
				writer.Write(" class ");
				writer.Write(className);
				writer.Write('{');
			}
			writer.WriteLine();
			writer.Write(bones[1]);
		}

		private bool MakeNamedExpression(string line, Regex namedExpressionRx) {
			var match = namedExpressionRx.Match(line);
			if (match is not null) {
				if (match.Groups.Count == 3) {
					var name = match.Groups[1].Value;
					var rx = match.Groups[2].Value;
					RegularExpressionParser parser = new(new RegularExpressionScanner(rx), parameters);
					if (parser.Parse()) {
						if (parser.Result.CheckForEmpty()) {
							Console.Error.WriteLine($"named regular expression '{name}' accepts empty");
							return false;
						}
						namedExpressions.Add(name, parser.Result);
						return true;
					}
					Console.Error.WriteLine($"cannot parse named regular expression '{name}'");
					return false;
				}
			}
			Console.Error.WriteLine($"cannot recognize named regular expression");
			return false;
		}

		private string? StartWithRegularExpression(string line, Dictionary<Nfa, int> rules, List<StringBuilder> codes) {
			bool isEscaping = false;
			int index = 0;
			foreach (char ch in line) {
				if (isEscaping) {
					isEscaping = false;
				} else if (char.IsWhiteSpace(ch)) {
					break;
				} else if (ch == '\\') {
					isEscaping = true;
				}
				++index;
			}
			if (isEscaping) {
				return "unterminated escape in regular expression";
			} else if (index == line.Length) {
				return "no code after regular expression";
			}
			RegularExpressionParser parser = new(new RegularExpressionScanner(line[..index]), parameters);
			if (!parser.Parse()) {
				return "cannot parse regular expression";
			}
			return AddToRulesAndCodes(line, index, parser.Result, rules, codes);
		}

		private string? StartWithLiteral(string line, Dictionary<Nfa, int> rules, List<StringBuilder> codes) {
			Nfa nfa = new(new EpsilonSymbol());
			bool isEscaping = false;
			int index = 1;
			foreach (char ch in line.Skip(1)) {
				++index;
				if (isEscaping) {
					isEscaping = false;
					if (!RegularExpressionScanner.knownEscapes.TryGetValue(ch, out char escape)) {
						escape = ch;
					}
					nfa += new Nfa(new SimpleSymbol(escape));
				} else if (ch == '"') {
					break;
				} else if (ch == '\\') {
					isEscaping = true;
				} else {
					nfa += new Nfa(new SimpleSymbol(ch));
				}
			}
			if (isEscaping) {
				return "unterminated escape in literal";
			}
			return AddToRulesAndCodes(line, index, nfa, rules, codes);
		}

		private string? AddToRulesAndCodes(string line, int index, Nfa nfa, Dictionary<Nfa, int> rules, List<StringBuilder> codes) {
			if (nfa.CheckForEmpty()) {
				return "regular expression accepts empty";
			}
			if (continuationRx.IsMatch(line, index)) {
				rules.Add(nfa, -1);
			} else {
				int codesIndex = codes.Count;
				foreach (var pair in rules.Where(p => p.Value == -1).ToArray()) {
					rules[pair.Key] = codesIndex;
				}
				rules.Add(nfa, codesIndex);
				StringBuilder sb = new(new string(';', index));
				sb.AppendLine(line[index..]);
				codes.Add(sb);
			}
			return null;
		}

		private enum State { InSectionOne, InSectionOneCode, InSectionTwo, InSectionThree }
	}
}
