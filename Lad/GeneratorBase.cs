using System.Text;

namespace Lad {
	public abstract class GeneratorBase {
		protected class StateMachine {
			public string MethodDeclarationText { get; }
			public string? MethodName { get; }
			public Dictionary<int, string>? LabelTexts { get; }
			public KeyValuePair<Nfa, int>[] Rules { get; }
			public string[] Codes { get; }
			public int? DefaultActionIndex { get; }

			public StateMachine(string methodDeclarationText, string? methodName, Dictionary<int, string>? labelTexts, IEnumerable<KeyValuePair<Nfa, int>> rules, IEnumerable<string> codes, int? defaultActionIndex) {
				MethodDeclarationText = methodDeclarationText;
				MethodName = methodName;
				LabelTexts = labelTexts;
				Rules = rules.ToArray();
				Codes = codes.ToArray();
				DefaultActionIndex = defaultActionIndex;
			}
		}

		protected readonly Dictionary<string, Nfa> namedExpressions = new();
		protected readonly bool isDebug;
		protected readonly RegularExpressionParser.Parameters parameters;
		protected string[] classAccesses;
		protected string[] classNames;
		protected string[] namespaceNames;
		private readonly string? inputFilePath;
		private readonly string? outputFilePath;
		private readonly string[] bones;
		private readonly bool wantsLineNumbers;

		protected GeneratorBase(Options options) {
			parameters = new RegularExpressionParser.Parameters(namedExpressions, options.DotIncludesNewline, options.NewLine);
			namespaceNames = options.NamespaceNames;
			classAccesses = options.ClassAccesses;
			classNames = options.ClassNames;
			inputFilePath = options.InputFilePath;
			outputFilePath = options.OutputFilePath;
			wantsLineNumbers = options.WantsLineNumbers;
			isDebug = options.IsDebug;
			string skeleton = Properties.Resources.Skeleton;
			string[] parts = skeleton.Split('$');
			bones = parts.Select(s => s[..(s.LastIndexOf('\n') + 1)]).ToArray();
		}

		public bool Generate() {
			string text = inputFilePath == null ? Console.In.ReadToEnd() : File.ReadAllText(inputFilePath);
			IEnumerable<StateMachine>? stateMachines = ProcessInput(text);
			if (stateMachines == null) {
				return false;
			}
			StringWriter writer = new();
			if (wantsLineNumbers) {
				writer.WriteLine("#define LAD_WANTS_LINE_NUMBERS");
			}
			writer.Write(bones[0]);
			WriteHeader(writer);
			foreach (string namespaceName in namespaceNames) {
				writer.Write($"namespace {namespaceName}{{");
			}
			var classPairs = classAccesses.Zip(classNames);
			foreach ((string classAccess, string className) in classPairs) {
				writer.Write($"{classAccess} class {className}{{");
			}
			writer.WriteLine();
			writer.Write(bones[1]);
			if (stateMachines.All((s) => s.LabelTexts is not null)) {
				writer.WriteLine("Dictionary<int,string>[]actionMap_=new Dictionary<int,string>[]{");
				foreach (StateMachine stateMachine in stateMachines) {
					writer.WriteLine("new(){");
					foreach (KeyValuePair<int, string> item in stateMachine.LabelTexts!) {
						writer.WriteLine($"{{{item.Key + 1},{item.Value}}},");
					}
					writer.WriteLine("},");
				}
				writer.WriteLine("};");
			}
			foreach ((StateMachine stateMachine, int index) in stateMachines.Select((s, i) => (s, i))) {
				WriteStateMachine(stateMachine.MethodDeclarationText, stateMachine.Rules.GroupBy(p => p.Value).Select(CombineNfas), writer);
				WriteActions(index, stateMachine, writer);
				writer.WriteLine("break;}}}");
			}
			WriteFooter(writer);
			writer.WriteLine(new string('}', namespaceNames.Length + classNames.Length));
			WriteOutput(writer, outputFilePath);
			return true;
		}

		protected abstract void WriteActions(int index, StateMachine stateMachine, StringWriter writer);
		protected abstract void WriteFooter(StringWriter writer);
		protected abstract void WriteHeader(StringWriter writer);
		protected abstract IEnumerable<StateMachine>? ProcessInput(string text);

		private static string MakeIfStatement(string name, KeyValuePair<ConcreteSymbol, DfaState> pair, string defaultState, Func<string, string> makeState, ref bool needsExit) {
			string expression = pair.Key.MakeExpression("ch");
			string targetState = makeState(pair.Value.Name);
			var statement = new StringBuilder("if(").Append(expression).Append("){");
			if (pair.Value.SaveForAcceptance != 0) {
				statement.Append("saves[-").Append(pair.Value.SaveForAcceptance).Append("]=reader_.Position-1;");
			}
			if (pair.Value.Acceptance != 0) {
				statement.Append("saves[").Append(pair.Value.Acceptance).Append("]=reader_.Position;");
			}
			if (pair.Value.Acceptance == 0 || pair.Value.Transitions.Any()) {
				if (name != pair.Value.Name) {
					statement.Append("state=").Append(targetState).Append(';');
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
#if DEBUG
				startState.Dump(sb, true);
#else
				startState.Dump(sb);
#endif
				Console.Error.Write(sb.ToString());
			}
			writer.WriteLine($"{methodDeclarationText}{{");
			writer.WriteLine("Dictionary<int,int>saves=new Dictionary<int,int>();");
			writer.WriteLine("if(reader_==null)reader_=new Reader_(reader);");
			string startStateName = makeState(startState.Name);
			writer.WriteLine($"for(var state={startStateName};;){{");
			writer.WriteLine("#if LAD_WANTS_LINE_NUMBERS");
			writer.WriteLine("LineNumber=nextLineNumber;");
			writer.WriteLine("#endif");
			writer.WriteLine("int ch=reader_.Read();");
			writer.WriteLine("switch(state){");
			WriteTransitions(startState, new HashSet<string>(), defaultState, makeState, writer);
			writer.WriteLine($"case {defaultState}:");
			writer.WriteLine("var longest=saves.Where(p=>p.Key>0).Aggregate(new KeyValuePair<int,int>(int.MaxValue,1),(a,b)=>a.Value<b.Value?b:b.Value<a.Value?a:a.Key<b.Key?a:b);");
			writer.WriteLine("if(!saves.TryGetValue(-longest.Key,out int consumptionValue)||longest.Value<consumptionValue)consumptionValue=longest.Value;");
			writer.WriteLine($"string tokenValue=reader_.Consume(consumptionValue);saves.Clear();state={startStateName};");
			writer.Write("if(!tokenValue.Any())return");
			if (!methodDeclarationText.Contains(" void ")) {
				writer.Write(" default");
			}
			writer.WriteLine(";");
			writer.WriteLine("#if LAD_WANTS_LINE_NUMBERS");
			writer.WriteLine(@"nextLineNumber+=tokenValue.Count(c=>c=='\n');");
			writer.WriteLine("#endif");
		}

		private static void WriteOutput(StringWriter writer, string? outputFilePath) {
			using TextWriter streamWriter = outputFilePath == null ? Console.Out : new StreamWriter(outputFilePath, false);
			string s = writer.ToString();
			streamWriter.Write(s);
		}

		private Nfa CombineNfas(IGrouping<int, KeyValuePair<Nfa, int>> groups) {
			Nfa rv = Nfa.Or(groups.Select(p => p.Key).ToArray());
			int acceptanceValue = groups.Key + 1;
			rv += new Nfa(new AcceptingSymbol(acceptanceValue));
			rv.SetSavePoint(acceptanceValue);
			rv.RemoveEpsilonTransitions();
			if (isDebug) {
				Console.Error.WriteLine($"for acceptance value {acceptanceValue}:");
				Console.Error.WriteLine(rv.Dump());
			}
			return rv;
		}

		private static void WriteTransitions(DfaState dfa, HashSet<string> hashSet, string defaultState, Func<string, string> makeState, StringWriter writer) {
			if (hashSet.Add(dfa.Name)) {
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
