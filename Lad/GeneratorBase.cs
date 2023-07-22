using System.Text;
using System.Text.RegularExpressions;

namespace Lad {
	internal abstract class GeneratorBase<T> {
		protected readonly Dictionary<string, Nfa> namedExpressions = new();
		protected Options options;

		protected GeneratorBase(Options options) {
			// Derivations parse options in their constructors.
			this.options = options;
		}

		public bool Generate() {
			string text = options.InputFilePath == null ? Console.In.ReadToEnd() : File.ReadAllText(options.InputFilePath);
			(IEnumerable<KeyValuePair<Nfa, int>>? rules, IEnumerable<string>? codes) = ProcessInput(text);
			if (rules == null || codes == null) {
				return false;
			}
			StringWriter writer = new();
			T state = WriteHeader(writer);
			WriteStateMachine(rules.GroupBy(p => p.Value).Select(CombineNfas), writer);
			WriteActions(codes, writer);
			WriteFooter(state, writer);
			WriteOutput(writer, options.OutputFilePath);
			return true;
		}

		protected abstract void WriteFooter(T state, StringWriter writer);
		protected abstract T WriteHeader(StringWriter writer);
		protected abstract (IEnumerable<KeyValuePair<Nfa, int>>? rules, IEnumerable<string>? codes) ProcessInput(string text);

		private static string MakeIfStatement(string name, KeyValuePair<ConcreteSymbol, DfaState> pair, string defaultState, Func<string, string> makeState, ref bool needsBreak) {
			string expression = pair.Key.MakeExpression("ch_");
			string statement = $"if({expression}){{";
			string s = name == pair.Value.Name ? "continue" : $"state_={makeState(pair.Value.Name)}";
			statement += pair.Value.SaveForAcceptance == 0 ? "" : $"saves_[-{pair.Value.SaveForAcceptance}]=reader_.Position;";
			if (pair.Value.Acceptance == 0) {
				needsBreak |= s != "continue";
				return $"{statement}{s};}}";
			} else {
				string savesStatement = $"saves_[{pair.Value.Acceptance}]=reader_.Position;";
				if (pair.Value.Transitions.Any()) {
					needsBreak |= s != "continue";
					return $"{statement}{savesStatement}{s};}}";
				} else {
					return $"{statement}{savesStatement}state_={defaultState};goto case {defaultState};}}";
				}
			}
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
			writer.WriteLine($"var state_={makeState(startState.Name)};Dictionary<int,int>saves_=new Dictionary<int,int>();");
			writer.WriteLine($"System.Func<KeyValuePair<int,int>,int>fn_=p=>{valueMultiplier}*p.Value-p.Key;");
			writer.WriteLine("for(;;){");
			writer.WriteLine("int ch_=reader_.Read();");
			writer.WriteLine("switch(state_){");
			WriteTransitions(startState, new HashSet<string>(), defaultState, makeState, writer);
			writer.WriteLine($"case {defaultState}:");
			writer.WriteLine("var longest_=saves_.Where(p=>p.Key>0).Aggregate(new KeyValuePair<int,int>(int.MaxValue,1),(a,b)=>a.Value<b.Value?b:b.Value<a.Value?a:a.Key<b.Key?a:b);");
			writer.WriteLine("if(!saves_.TryGetValue(-longest_.Key,out int consumptionValue))consumptionValue=longest_.Value;");
			writer.WriteLine("string tokenValue=reader_.Consume(consumptionValue);");
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
					bool needsBreak = false;
					IEnumerable<string> ifs = dfa.Transitions.OrderBy(p => p.Key.Order).Select(p => MakeIfStatement(dfa.Name, p, defaultState, makeState, ref needsBreak));
					string s = string.Join("else ", ifs);
					if (dfa.Transitions.All(p => p.Key is not AnySymbol)) {
						s += $"else{{state_={defaultState};goto case {defaultState};}}";
					}
					writer.WriteLine(s);
					if (needsBreak) {
						writer.WriteLine("break;");
					}
					foreach (KeyValuePair<ConcreteSymbol, DfaState> transition in dfa.Transitions) {
						WriteTransitions(transition.Value, hashSet, defaultState, makeState, writer);
					}
				}
			}
		}
	}
}
