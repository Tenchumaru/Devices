using System.Text;
using System.Text.RegularExpressions;

namespace Lad {
	public class LexGenerator : GeneratorBase, IGenerator {
		private readonly Regex continuationRx = new(@"\s*\|");
		private readonly Regex namedExpressionRx = new(@"^([A-Za-z]\w+)\s+(.+)");
		private readonly StringBuilder sectionOneCode = new();
		private readonly StringBuilder moreCode = new();
		private readonly int? tabStop;
		private readonly List<string> defineDirectives;
		private readonly List<string> additionalUsingDirectives;
		private readonly string classDeclaration;

		public LexGenerator(Options options) : base(options) {
			tabStop = options.TabStop;
			defineDirectives = options.DefineDirectives;
			additionalUsingDirectives = options.AdditionalUsingDirectives;
			classDeclaration = options.ClassDeclaration;
		}

		protected override IEnumerable<StateMachine>? ProcessInput(string text) {
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
			return foundError ? default : new[] { new StateMachine("internal Token? Read()", rules, codes.Select(s => s.ToString()), null) };
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
			writer.WriteLine($"{classDeclaration}{{");
			writer.Write(bones[1]);
		}

		protected override void WriteFooter(StringWriter writer) {
			writer.Write(moreCode.ToString());
			writer.WriteLine(bones[3]);
			writer.WriteLine(new string('}', classDeclaration.Split('{').Length - 1));
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
				if (tabStop > 0) {
					string s = new string(' ', index) + line[index..];
					string ws = s[..^s.TrimStart().Length];
					ws = string.Join("", ws.Chunk(tabStop.Value).Select(a => a.All(c => c == ' ') ? "\t" : string.Join("", a.SkipWhile(c => c == ' '))));
					StringBuilder sb = new(ws);
					sb.AppendLine(s.TrimStart());
					codes.Add(sb);
				} else {
					StringBuilder sb = new(new string(' ', index));
					sb.AppendLine(line[index..]);
					codes.Add(sb);
				}
			}
			return null;
		}

		private enum State { InSectionOne, InSectionOneCode, InSectionTwo, InSectionThree }
	}
}
