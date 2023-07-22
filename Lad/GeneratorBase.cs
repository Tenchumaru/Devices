using System.Text;
using System.Text.RegularExpressions;

namespace Lad {
	internal abstract class GeneratorBase {
		protected readonly Dictionary<string, Nfa> namedExpressions = new();
		protected Options options;
		protected string[] bones;

		protected GeneratorBase(Options options) {
			// Derivations parse options in their constructors.
			this.options = options;
			string skeleton = Properties.Resources.Skeleton;
			var parts = skeleton.Split('$');
			bones = parts.Select(s => s[..(s.LastIndexOf('\n') + 1)]).ToArray();
		}

		public bool Generate() {
			string text = options.InputFilePath == null ? Console.In.ReadToEnd() : File.ReadAllText(options.InputFilePath);
			(IEnumerable<KeyValuePair<Nfa, int>>? rules, IEnumerable<string>? codes) = ProcessInput(text);
			if (rules == null || codes == null) {
				return false;
			}
			StringWriter writer = new();
			WriteHeader(writer);
			WriteStateMachine(rules.GroupBy(p => p.Value).Select(CombineNfas), writer);
			WriteActions(codes, writer);
			WriteFooter(writer);
			WriteOutput(writer, options.OutputFilePath);
			return true;
		}

		protected abstract void WriteFooter(StringWriter writer);
		protected abstract void WriteHeader(StringWriter writer);
		protected abstract (IEnumerable<KeyValuePair<Nfa, int>>? rules, IEnumerable<string>? codes) ProcessInput(string text);

		private static string MakeIfStatement(string name, KeyValuePair<ConcreteSymbol, DfaState> pair, string defaultState, Func<string, string> makeState, ref bool needsExit) {
			string expression = pair.Key.MakeExpression("ch_");
			string targetState = makeState(pair.Value.Name);
			var statement = new StringBuilder("if(").Append(expression).Append("){");
			if (pair.Value.SaveForAcceptance != 0) {
				statement.Append("saves_[-").Append(pair.Value.SaveForAcceptance).Append("]=reader_.Position;");
			}
			if (pair.Value.Acceptance != 0) {
				statement.Append("saves_[").Append(pair.Value.Acceptance).Append("]=reader_.Position;");
			}
			if (pair.Value.Acceptance == 0 || pair.Value.Transitions.Any()) {
				if (name != pair.Value.Name) {
					statement.Append("state_=").Append(targetState);
				}
				if (pair.Key is BolSymbol) {
					statement.Append(";goto case ").Append(targetState);
				} else {
					needsExit = true;
				}
			} else {
				statement.Append("goto case ").Append(defaultState);
			}
			statement.Append(";}");
			return statement.ToString();
		}

		protected bool MakeNamedExpression(string line, Regex namedExpressionRx) {
			var match = namedExpressionRx.Match(line);
			if (match is not null) {
				if (match.Groups.Count == 3) {
					var name = match.Groups[1].Value;
					var rx = match.Groups[2].Value;
					RegularExpressionParser parser = new(new RegularExpressionScanner(rx), namedExpressions);
					if (parser.Parse()) {
						namedExpressions.Add(name, parser.Result);
						return true;
					}
				}
			}
			Console.Error.WriteLine($"cannot parse named expression");
			return false;
		}

		protected void WriteStateMachine(IEnumerable<Nfa> nfas, StringWriter writer) {
			Nfa[] array = nfas.ToArray();
			int valueMultiplier = array.Length + 1;
			Nfa nfa = Nfa.Or(array);
			(DfaState startState, Dictionary<string, int> caseValues) = nfa.MakeDfa();
			string defaultState = options.IsDebug ? "null" : "0";
			string makeState(string s) => options.IsDebug ? $"\"{s}\"" : $"{caseValues[s]}";
			Console.Error.WriteLine("DFA:");
			StringBuilder sb = new();
			startState.Dump(sb);
			Console.Error.WriteLine(sb.ToString());
			writer.WriteLine("if(reader_==null)reader_=new Reader_(reader);");
			string startStateName = makeState(startState.Name);
			writer.WriteLine($"var state_={startStateName};");
			writer.WriteLine("for(;;){");
			writer.WriteLine("int ch_=reader_.Read();");
			writer.WriteLine("switch(state_){");
			WriteTransitions(startState, new HashSet<string>(), defaultState, makeState, writer);
			writer.WriteLine($"case {defaultState}:");
			writer.WriteLine("var longest_=saves_.Where(p=>p.Key>0).Aggregate(new KeyValuePair<int,int>(int.MaxValue,1),(a,b)=>a.Value<b.Value?b:b.Value<a.Value?a:a.Key<b.Key?a:b);");
			writer.WriteLine("if(!saves_.TryGetValue(-longest_.Key,out int consumptionValue))consumptionValue=longest_.Value;");
			writer.WriteLine($"string tokenValue=reader_.Consume(consumptionValue);saves_.Clear();state_={startStateName};");
			writer.WriteLine("if(!tokenValue.Any())return default;");
			writer.WriteLine("switch(longest_.Key){");
		}

		protected static void WriteActions(IEnumerable<string> codes, StringWriter writer) {
			writer.WriteLine("#pragma warning disable CS0162 // Unreachable code detected");
			foreach ((string code, int ruleNumber) in codes.Select((s, i) => (s, i + 1))) {
				writer.WriteLine($"case {ruleNumber}:");
				writer.WriteLine(code);
				writer.WriteLine("break;");
			}
			writer.WriteLine("#pragma warning restore CS0162 // Unreachable code detected");
		}

		protected static void WriteOutput(StringWriter writer, string? outputFilePath) {
			using TextWriter streamWriter = outputFilePath == null ? Console.Out : new StreamWriter(outputFilePath, false);
			string s = writer.ToString();
			streamWriter.Write(s);
		}

		protected static Nfa CombineNfas(IGrouping<int, KeyValuePair<Nfa, int>> groups) {
			var rv = Nfa.Or(groups.Select(p => p.Key).ToArray());
			int acceptanceValue = groups.Key + 1;
			Console.Error.WriteLine($"for acceptance value {acceptanceValue}:");
			StringBuilder sb = new();
			rv.Dump(sb);
			Console.Error.WriteLine(sb.ToString());
			rv += new Nfa(new AcceptingSymbol(acceptanceValue));
			rv.SetSavePointValue(acceptanceValue);
			return rv;
		}

		private void WriteTransitions(DfaState dfa, HashSet<string> hashSet, string defaultState, Func<string, string> makeState, StringWriter writer) {
			if (!hashSet.Contains(dfa.Name)) {
				hashSet.Add(dfa.Name);
				if (dfa.Transitions.Any()) {
					writer.WriteLine($"case {makeState(dfa.Name)}:");
					bool needsExit = false;
					IEnumerable<string> ifs = dfa.Transitions.OrderBy(p => p.Key.Order).Select(p => MakeIfStatement(dfa.Name, p, defaultState, makeState, ref needsExit));
					string s = string.Join("else ", ifs);
					if (dfa.Transitions.All(p => p.Key is not AnySymbol)) {
						s += $"else goto case {defaultState};";
					}
					writer.WriteLine(s);
					if (needsExit) {
						writer.WriteLine("continue;");
					}
					foreach (KeyValuePair<ConcreteSymbol, DfaState> transition in dfa.Transitions) {
						WriteTransitions(transition.Value, hashSet, defaultState, makeState, writer);
					}
				}
			}
		}
	}
}
