using System.Text;

namespace Lad {
	public abstract class GeneratorBase {
		protected class StateMachine {
			public string MethodDeclarationText;
			public KeyValuePair<Nfa, int>[] Rules { get; }
			public string[] Codes { get; }
			public int? DefaultActionIndex { get; }

			public StateMachine(string methodDeclarationText, IEnumerable<KeyValuePair<Nfa, int>> rules, IEnumerable<string> codes, int? defaultActionIndex) {
				MethodDeclarationText = methodDeclarationText;
				Rules = rules.ToArray();
				Codes = codes.ToArray();
				DefaultActionIndex = defaultActionIndex;
			}
		}

		protected readonly Dictionary<string, Nfa> namedExpressions = new();
		protected readonly string[] bones;
		protected readonly bool isDebug;
		protected readonly RegularExpressionParser.Parameters parameters;
		private readonly string? inputFilePath;
		private readonly string? outputFilePath;

		protected GeneratorBase(Options options) {
			// Derivations parse options in their constructors.
			parameters = new RegularExpressionParser.Parameters(namedExpressions, options.DotIncludesNewline);
			inputFilePath = options.InputFilePath;
			outputFilePath = options.OutputFilePath;
			isDebug = options.IsDebug;
			string skeleton = Properties.Resources.Skeleton;
			var parts = skeleton.Split('$');
			bones = parts.Select(s => s[..(s.LastIndexOf('\n') + 1)]).ToArray();
		}

		public bool Generate() {
			string text = inputFilePath == null ? Console.In.ReadToEnd() : File.ReadAllText(inputFilePath);
			IEnumerable<StateMachine>? stateMachines = ProcessInput(text);
			if (stateMachines == null) {
				return false;
			}
			StringWriter writer = new();
			WriteHeader(writer);
			foreach (var stateMachine in stateMachines) {
				WriteStateMachine(stateMachine.MethodDeclarationText, stateMachine.Rules.GroupBy(p => p.Value).Select(CombineNfas), writer);
				WriteActions(stateMachine, writer);
			}
			WriteFooter(writer);
			WriteOutput(writer, outputFilePath);
			return true;
		}

		protected abstract void WriteFooter(StringWriter writer);
		protected abstract void WriteHeader(StringWriter writer);
		protected abstract IEnumerable<StateMachine>? ProcessInput(string text);

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
					statement.Append("state_=").Append(targetState).Append(';');
				}
				if (pair.Key is BolSymbol) {
					statement.Append("goto case ").Append(targetState).Append(';');
				} else {
					needsExit = true;
				}
			} else {
				statement.Append("goto case ").Append(defaultState).Append(';');
			}
			statement.Append('}');
			return statement.ToString();
		}

		private void WriteStateMachine(string methodDeclarationText, IEnumerable<Nfa> nfas, StringWriter writer) {
			Nfa[] array = nfas.ToArray();
			int valueMultiplier = array.Length + 1;
			Nfa nfa = Nfa.Or(array);
			(DfaState startState, Dictionary<string, int> caseValues) = nfa.MakeDfa();
			string defaultState = isDebug ? "null" : "0";
			string makeState(string s) => isDebug ? $"\"{s}\"" : $"{caseValues[s]}";
			if (isDebug) {
				Console.Error.WriteLine("DFA:");
				StringBuilder sb = new();
				startState.Dump(sb);
				Console.Error.WriteLine(sb.ToString());
			}
			writer.WriteLine($"{methodDeclarationText}{{");
			writer.WriteLine("Dictionary<int,int>saves_=new Dictionary<int,int>();");
			writer.WriteLine("if(reader_==null)reader_=new Reader_(reader);");
			string startStateName = makeState(startState.Name);
			writer.WriteLine($"for(var state_={startStateName};;){{");
			writer.WriteLine("int ch_=reader_.Read();");
			writer.WriteLine("switch(state_){");
			WriteTransitions(startState, new HashSet<string>(), defaultState, makeState, writer);
			writer.WriteLine($"case {defaultState}:");
			writer.WriteLine("var longest_=saves_.Where(p=>p.Key>0).Aggregate(new KeyValuePair<int,int>(int.MaxValue,1),(a,b)=>a.Value<b.Value?b:b.Value<a.Value?a:a.Key<b.Key?a:b);");
			writer.WriteLine("if(!saves_.TryGetValue(-longest_.Key,out int consumptionValue))consumptionValue=longest_.Value;");
			writer.WriteLine($"string tokenValue=reader_.Consume(consumptionValue);saves_.Clear();state_={startStateName};");
			writer.Write("if(!tokenValue.Any())return");
			if (!methodDeclarationText.Contains(" void ")) {
				writer.Write(" default");
			}
			writer.WriteLine(";");
			writer.WriteLine("switch(longest_.Key){");
		}

		private void WriteActions(StateMachine stateMachine, StringWriter writer) {
			writer.WriteLine("#pragma warning disable CS0162 // Unreachable code detected");
			foreach ((string code, int ruleNumber) in stateMachine.Codes.Select((s, i) => (s, i + 1))) {
				writer.WriteLine($"case {ruleNumber}:");
				if (ruleNumber == stateMachine.DefaultActionIndex + 1) {
					writer.WriteLine("default:");
				}
				writer.WriteLine(code);
				writer.WriteLine("break;");
			}
			writer.Write(bones[2]);
			writer.WriteLine("#pragma warning restore CS0162 // Unreachable code detected");
		}

		private static void WriteOutput(StringWriter writer, string? outputFilePath) {
			using TextWriter streamWriter = outputFilePath == null ? Console.Out : new StreamWriter(outputFilePath, false);
			string s = writer.ToString();
			streamWriter.Write(s);
		}

		private Nfa CombineNfas(IGrouping<int, KeyValuePair<Nfa, int>> groups) {
			var rv = Nfa.Or(groups.Select(p => p.Key).ToArray());
			int acceptanceValue = groups.Key + 1;
			rv += new Nfa(new AcceptingSymbol(acceptanceValue));
			rv.SetSavePointValue(acceptanceValue);
#if DEBUG
			if (isDebug) {
				Console.Error.WriteLine($"for acceptance value {acceptanceValue}:");
				Console.Error.WriteLine(rv.Dump());
			}
#endif
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
					if (!dfa.Transitions.Any(p => p.Key == AnySymbol.Value)) {
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
