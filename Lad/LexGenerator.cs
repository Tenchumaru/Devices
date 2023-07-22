using System.Text;
using System.Text.RegularExpressions;

namespace Lad {
	internal class LexGenerator : GeneratorBase, IGenerator {
		private readonly Regex continuationRx = new(@"\s*\|");
		private readonly Regex namedExpressionRx = new(@"^([A-Za-z]\w+)\s+(.+)");
		private readonly StringBuilder sectionOneCode = new();
		private readonly StringBuilder moreCode = new();
		private readonly int? tabStop;

		public LexGenerator(Options options) : base(options) => tabStop = options.TabStop;

		protected override (IEnumerable<KeyValuePair<Nfa, int>>? rules, IEnumerable<string>? codes) ProcessInput(string text) {
			string[] lines = text.Split('\n').Select(s => s.TrimEnd()).ToArray();
			bool foundError = false;
			Dictionary<Nfa, int> rules = new();
			List<StringBuilder> codes = new();
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
						} else if (line == "" || line[0] == '/') {
							// Ignore.
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
						} else if (line[0] == '"') {
							string? s = StartWithLiteral(line, rules, codes);
							if (s != null) {
								Console.Error.WriteLine($"{s} in line {lineNumber}");
								foundError = true;
							}
						} else if (line == "%%") {
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
			return foundError ? default : (rules, codes.Select(s => s.ToString()));
		}

		protected override void WriteHeader(StringWriter writer) {
			foreach (var directive in options.DefineDirectives) {
				writer.WriteLine(directive);
			}
			foreach (var directive in options.AdditionalUsingDirectives) {
				writer.WriteLine(directive);
			}
			writer.Write(bones[0]);
			if (options.NamespaceName != null) {
				writer.WriteLine($"namespace {options.NamespaceName} {{");
			}
			writer.WriteLine($"{options.ScannerClassAccess} partial class {options.ScannerClassName} {{");
			writer.Write(sectionOneCode.ToString());
			writer.Write(bones[1]);
			writer.WriteLine("internal Token Read() {");
		}

		protected override void WriteFooter(StringWriter writer) {
			writer.WriteLine(bones[2]);
			writer.Write(moreCode.ToString());
			writer.WriteLine(bones[3]);
			if (options.NamespaceName != null) {
				writer.WriteLine('}');
			}
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
			RegularExpressionParser parser = new(new RegularExpressionScanner(line[..index]), namedExpressions);
			if (!parser.Parse()) {
				return "cannot parse regular expression";
			}
			AddToRulesAndCodes(line, index, parser.Result, rules, codes);
			return null;
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
			AddToRulesAndCodes(line, index, nfa, rules, codes);
			return null;
		}

		private void AddToRulesAndCodes(string line, int index, Nfa nfa, Dictionary<Nfa, int> rules, List<StringBuilder> codes) {
			if (continuationRx.IsMatch(line, index)) {
				rules.Add(nfa, -1);
			} else {
				int codesIndex = codes.Count;
				foreach (var pair in rules.Where(p => p.Value == -1).ToArray()) {
					rules[pair.Key] = codesIndex;
				}
				rules.Add(nfa, codesIndex);
				if (tabStop > 0) {
					string s = new string(' ', index) + line[index..];
					s = string.Join("", s.Chunk(tabStop.Value).Select(a => a.All(c => c == ' ') ? "\t" : new string(a)));
					codes.Add(new StringBuilder(s));
				} else {
					StringBuilder sb = new(new string(' ', index));
					sb.AppendLine(line[index..]);
					codes.Add(sb);
				}
			}
		}

		private enum State { InSectionOne, InSectionOneCode, InSectionTwo, InSectionThree }
	}
}
