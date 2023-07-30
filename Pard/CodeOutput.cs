using System.Text;

namespace Pard {
	class CodeOutput : IGrammarOutput {
		private static readonly string[] requiredUsingDirectives = {
			"System",
			"System.Collections.Generic",
			"System.Linq",
		};

		public void Write(IReadOnlyList<Grammar.ActionEntry> actions, IReadOnlyList<Grammar.GotoEntry> gotos, IReadOnlyList<Production> productions, TextWriter writer, Options options) {
			// Emit the define directives.
			foreach (string defineDirective in options.DefineDirectives) {
				writer.WriteLine("#define " + defineDirective);
			}

			writer.WriteLine("#nullable enable");

			// Get the output skeleton.
			StringReader skeleton = new(Properties.Resources.Skeleton);
			EmitSection(skeleton, writer);

			// Add unique additional using directives.
			string[] uniqueUsingDirectives = options.AdditionalUsingDirectives.Except(requiredUsingDirectives).ToArray();
			if (uniqueUsingDirectives.Length > 0) {
				writer.WriteLine("using {0};", string.Join(";using ", uniqueUsingDirectives));
			}

			// Emit the namespace, class name, and constructor values.
			string classDeclaration = options.ClassDeclaration.Trim();
			string parserClassName = classDeclaration.Split(' ').Last();
			writer.WriteLine(classDeclaration);
			EmitSection(skeleton, writer);
			writer.WriteLine("private {0} scanner;", options.ScannerClassName);
			EmitSection(skeleton, writer);
			writer.WriteLine("#pragma warning disable CS8618");
			writer.WriteLine("public {0}({1} scanner)", parserClassName, options.ScannerClassName);
			writer.WriteLine("#pragma warning restore CS8618");
			EmitSection(skeleton, writer);

			// Map the non-terminal symbols to go-to and reduction table indices.
			Dictionary<Nonterminal, int> nonTerminalIndices = gotos.Select(e => e.Nonterminal).Distinct().Select((n, i) => new { N = n, I = i }).ToDictionary(a => a.N, a => a.I);

			// Construct the go-to table and replace it in the skeleton.
			string goTos = ConstructGoTos(gotos, nonTerminalIndices);
			writer.WriteLine(goTos);
			EmitSection(skeleton, writer);

			// Map the productions to reduction table entries.
			var reducedProductions = from e in actions
															 where e.Action == Grammar.Action.Reduce
															 select productions[e.Value];
			Dictionary<Production, int> productionIndices = reducedProductions.Distinct().Select((p, i) => new { P = p, I = i }).ToDictionary(a => a.P, a => a.I);

			// Construct the reduction table and replace it in the skeleton.
			string reductions = ConstructReductions(nonTerminalIndices, productionIndices);
			writer.WriteLine(reductions);
			EmitSection(skeleton, writer);

			int rowCount = actions.Max(e => e.StateIndex) + 1;
			var stateRowLists = new List<string>[rowCount];
			for (int i = 0; i < rowCount; ++i) {
				stateRowLists[i] = new List<string>();
			}

			foreach (Grammar.ActionEntry entry in actions) {
				Terminal terminal = entry.Terminal;
				switch (entry.Action) {
					case Grammar.Action.Accept:
						stateRowLists[entry.StateIndex].Add("case -1:return true;");
						break;
					case Grammar.Action.Reduce:
						stateRowLists[entry.StateIndex].Add(string.Format("case {0}:state_={1};goto reduce1;", terminal.Value, productionIndices[productions[entry.Value]]));
						break;
					case Grammar.Action.Shift:
						stateRowLists[entry.StateIndex].Add(string.Format("case {0}:state_={1};goto shift;", terminal.Value, entry.Value));
						break;
				}
			}

			// Collapse inner cases.
			List<string> stateRows = new();
			foreach (List<string> stateRowList in stateRowLists) {
				var inners = from s in stateRowList
										 let a = s.Split(':')
										 group a[0] by a[1] into g
										 select string.Format("{0}:{1}", string.Join(":", g.Distinct().ToArray()), g.Key);
				string joined = string.Join("", inners.ToArray());
				stateRows.Add(joined);
			}

			// Collapse outer cases and emit the transitions.
			var outers = stateRows.Select((s, i) => new { S = s, I = i }).GroupBy(a => a.S).
				Select(g => string.Format("case {0}:switch(token_.Symbol){{ {1} }}break;", string.Join(":case ", g.Select(a => a.I.ToString()).Distinct().ToArray()), g.Key));
			writer.WriteLine(string.Join(writer.NewLine, outers.ToArray()));
			EmitSection(skeleton, writer);

			if (!options.WantsWarnings) {
				writer.WriteLine("#pragma warning disable CS0162 // Unreachable code detected");
				writer.WriteLine("#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.");
				writer.WriteLine("#pragma warning disable CS8602 // Dereference of a possibly null reference.");
				writer.WriteLine("#pragma warning disable CS8604 // Possible null reference argument.");
				writer.WriteLine("#pragma warning disable CS8605 // Unboxing a possibly null value.");
			}

			// Emit the actions.
			int actionIndex = -1;
			foreach (var pair in productionIndices.Keys.Where(p => p.ActionCode != null).Select(p => new { Production = p, ActionCode = p.ActionCode! })) {
				writer.WriteLine("case {1}:{0}{2}{0}goto reduce2;", writer.NewLine, --actionIndex, ConstructAction(pair.Production, pair.ActionCode, options.LineDirectivesFilePath));
			}

			if (!options.WantsWarnings) {
				writer.WriteLine("#pragma warning restore CS8605 // Unboxing a possibly null value.");
				writer.WriteLine("#pragma warning restore CS8604 // Possible null reference argument.");
				writer.WriteLine("#pragma warning restore CS8602 // Dereference of a possibly null reference.");
				writer.WriteLine("#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.");
				writer.WriteLine("#pragma warning restore CS0162 // Unreachable code detected");
			}

			EmitSection(skeleton, writer);

			// Emit any terminal definitions.
			var terminals = from e in actions
											let t = e.Terminal
											where t.Name[0] != '\'' && t.Name != "(end)"
											select t;
			foreach (Terminal terminal in terminals.Distinct()) {
				writer.WriteLine("public const int {0}= {1};", terminal.Name, terminal.Value);
			}
			EmitSection(skeleton, writer);

			// Emit the token class, if requested.
			if (options.WantsTokenClass) {
				writer.WriteLine("public partial class Token { public int Symbol; public object Value; }");
			}
			foreach (string _ in classDeclaration.Split('{')) {
				writer.WriteLine('}');
			}
		}

		private static string ConstructAction(Production production, ActionCode actionCode, string? lineDirectivesFilePath) {
			string code = actionCode.Code.Replace("$$.", string.Format("(({0})reductionValue_).", production.Lhs.TypeName));
			code = code.Replace("$$", "reductionValue_");
			string[] parts = code.Split('$');

			for (int i = 1; i < parts.Length; ++i) {
				string part = parts[i];
				if (part.Length < 1) {
					throw MakeMalformedSubstitutionException(production);
				}

				// Parse any explicit type name.
				string? typeName = null;
				int j = 0;
				if (part[0] == '<') {
					j = part.IndexOf('>');
					if (j < 2) {
						throw MakeMalformedSubstitutionException(production);
					}
					typeName = part[1..j];
					++j;
				}

				// Parse the symbol number.
				if (j >= part.Length) {
					throw MakeMalformedSubstitutionException(production);
				}
				if (part[j] == '$') {
					parts[i] = typeName != null ?
						string.Format("(({0})reductionValue_{1})", typeName, part[(j + 1)..]) :
						"reductionValue_" + part[(j + 1)..];
					continue;
				}
				StringBuilder sb = new();
				if (part[j] == '-') {
					sb.Append('-');
					++j;
				}
				for (; j < part.Length && char.IsDigit(part[j]); ++j) {
					sb.Append(part[j]);
				}
				if (!int.TryParse(sb.ToString(), out int symbolNumber)) {
					throw MakeMalformedSubstitutionException(production);
				}
				int symbolIndex = symbolNumber - 1;
				if (symbolIndex >= production.Rhs.Count) {
					throw new ApplicationException("invalid substitution in action for " + production);
				}

				// Replace the specifier with the stack accessor.
				typeName ??= production.Rhs[symbolIndex].TypeName;
				if (typeName == null) {
					Console.WriteLine("warning: type name not specified; using object");
					typeName = "object";
				}
				int stackIndex = production.Rhs.Count - symbolIndex;
				string s = string.Format("(({0})(stack_[stack_.Count - {1}].Value))", typeName, stackIndex);
				parts[i] = s + part[j..];
			}

			code = string.Join("", parts);
			if (lineDirectivesFilePath != null) {
				code = string.Format("#line {1} \"{2}\"{0}{3}{0}#line default{0}", Environment.NewLine, actionCode.LineNumber, lineDirectivesFilePath, code);
			}
			return code;
		}

		private static void EmitSection(StringReader skeleton, TextWriter output) {
			for (string? line = skeleton.ReadLine(); line != null && !line.EndsWith("$"); line = skeleton.ReadLine()) {
				output.WriteLine(line);
			}
		}

		private static string ConstructReductions(Dictionary<Nonterminal, int> nonTerminalIndices, Dictionary<Production, int> productionIndices) {
			StringBuilder sb = new();
			int actionIndex = -1;
			foreach (Production production in productionIndices.Keys) {
				if (production.ActionCode != null) {
					sb.AppendFormat("new R_({0},{1},{2}),", nonTerminalIndices[production.Lhs], production.Rhs.Count, --actionIndex);
				} else {
					string? rhsTypeName = production.Rhs.Count == 0 ? null : production.Rhs[0].TypeName;
					if (production.Lhs.TypeName != rhsTypeName) {
						Console.WriteLine("warning: default action type mismatch; assigning '{0}' from '{1}'", production.Lhs.TypeName, rhsTypeName);
					}
					sb.AppendFormat("new R_({0},{1}),", nonTerminalIndices[production.Lhs], production.Rhs.Count);
				}
			}
			return sb.ToString();
		}

		private static string ConstructGoTos(IReadOnlyList<Grammar.GotoEntry> gotos, Dictionary<Nonterminal, int> nonTerminalIndices) {
			var goTos = from g in gotos
									let r = g.StateIndex
									let i = nonTerminalIndices[g.Nonterminal]
									orderby r, i
									select new { Row = r, Column = i, Target = g.TargetStateIndex };
			if (!goTos.Any()) {
				return "";
			}
			int goToRowCount = goTos.Max(g => g.Row) + 1;
			var goToArray = new int[goToRowCount, nonTerminalIndices.Count];
			foreach (var g in goTos) {
				goToArray[g.Row, g.Column] = g.Target;
			}
			StringBuilder sb = new();
			for (int i = 0; i < goToRowCount; ++i) {
				sb.Append('{');
				for (int j = 0; j < nonTerminalIndices.Count; ++j) {
					sb.AppendFormat("{0},", goToArray[i, j]);
				}
				--sb.Length;
				sb.Append("},");
			}
			--sb.Length;
			return sb.ToString();
		}

		private static ApplicationException MakeMalformedSubstitutionException(Production production) => new ApplicationException("malformed substitution in " + production);
	}
}
