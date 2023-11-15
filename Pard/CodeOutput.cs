using System.Text;

namespace Pard {
	class CodeOutput {
		private static readonly string[] requiredUsingDirectives = {
			"System",
			"System.Collections.Generic",
			"System.Linq",
		};
		private readonly IEnumerable<(string, int)> terminals;
		private readonly IReadOnlyList<Grammar.ActionEntry> actionEntries;
		private readonly IReadOnlyList<ActionCode> codeBlocks;
		private readonly IReadOnlyList<Grammar.GotoEntry> gotos;
		private readonly IReadOnlyList<Production> productions;
		private readonly Options options;

		public CodeOutput(IEnumerable<(string, int)> terminals, IReadOnlyList<Grammar.ActionEntry> actionEntries, IReadOnlyList<ActionCode> codeBlocks, IReadOnlyList<Grammar.GotoEntry> gotos, IReadOnlyList<Production> productions, Options options) {
			this.terminals = terminals;
			this.actionEntries = actionEntries;
			this.codeBlocks = codeBlocks;
			this.gotos = gotos;
			this.productions = productions;
			this.options = options;
		}

		public void Write(TextWriter writer) {
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
			if (uniqueUsingDirectives.Any()) {
				writer.WriteLine("using {0};", string.Join(";using ", uniqueUsingDirectives));
			}

			// Emit the namespace, class name, code blocks, and constructor values.
			foreach (string namespaceName in options.NamespaceNames) {
				writer.Write("namespace ");
				writer.Write(namespaceName);
				writer.Write('{');
			}
			var classPairs = options.ClassAccesses.Zip(options.ClassNames);
			foreach ((string classAccess, string className) in classPairs) {
				writer.Write(classAccess);
				writer.Write(" class ");
				writer.Write(className);
				writer.Write('{');
			}
			writer.WriteLine();
			EmitSection(skeleton, writer);
			writer.WriteLine("private {0} scanner;", options.ScannerClassName);
			EmitSection(skeleton, writer);
			foreach (ActionCode codeBlock in codeBlocks) {
				if (options.LineDirectivesFilePath != null) {
					writer.WriteLine($"#line {codeBlock.LineNumber} \"{options.LineDirectivesFilePath}\"");
				}
				writer.WriteLine(codeBlock.Code);
			}
			writer.WriteLine("#line default");
			writer.WriteLine("#pragma warning disable CS8618");
			string parserClassName = options.ClassNames.Last();
			writer.WriteLine("public {0}({1} scanner)", parserClassName, options.ScannerClassName);
			writer.WriteLine("#pragma warning restore CS8618");
			EmitSection(skeleton, writer);

			// Map the non-terminal symbols to go-to and reduction table indices.
			Dictionary<Nonterminal, int> nonTerminalIndices = gotos.Select(e => e.Nonterminal).Distinct().Select((n, i) => (n, i)).ToDictionary(a => a.n, a => a.i);

			// Construct the go-to table and replace it in the skeleton.
			string goTos = ConstructGoTos(gotos, nonTerminalIndices);
			writer.WriteLine(goTos);
			EmitSection(skeleton, writer);

			// Map the productions to reduction table entries.
			var reducedProductions = from e in actionEntries
															 where e.Action == Grammar.Action.Reduce
															 select productions[e.Value];
			Dictionary<Production, int> productionIndices = reducedProductions.Distinct().Select((p, i) => (p, i)).ToDictionary(a => a.p, a => a.i);

			// Construct the reduction table and replace it in the skeleton.
			string reductions = ConstructReductions(nonTerminalIndices, productionIndices);
			writer.WriteLine(reductions);
			EmitSection(skeleton, writer);

			int rowCount = actionEntries.Max(e => e.StateIndex) + 1;
			var stateRowLists = new List<string>[rowCount];
			for (int i = 0; i < rowCount; ++i) {
				stateRowLists[i] = new List<string>();
			}

			foreach (Grammar.ActionEntry entry in actionEntries) {
				Terminal terminal = entry.Terminal;
				switch (entry.Action) {
					case Grammar.Action.Accept:
						stateRowLists[entry.StateIndex].Add("case -1:return true;");
						break;
					case Grammar.Action.Reduce:
						stateRowLists[entry.StateIndex].Add($"case {terminal.Value}:state_={productionIndices[productions[entry.Value]]};goto reduce1;");
						break;
					case Grammar.Action.Shift:
						stateRowLists[entry.StateIndex].Add($"case {terminal.Value}:state_={entry.Value};goto shift;");
						break;
				}
			}

			// Collapse inner cases.
			List<string> stateRows = new();
			foreach (List<string> stateRowList in stateRowLists) {
				var inners = from s in stateRowList
										 let a = s.Split(':')
										 group a[0] by a[1] into g
										 let l = string.Join(":", g.Distinct().ToArray())
										 select $"{l}:{g.Key}";
				string joined = string.Join("", inners.ToArray());
				stateRows.Add(joined);
			}

			// Collapse outer cases and emit the transitions.
			var outers = from t in stateRows.Select((s, i) => (s, i))
									 group t by t.s into g
									 let c = string.Join(":case ", g.Select(a => a.i).Distinct())
									 select $"case {c}:switch(token_.Symbol){{ {g.Key} }}break;";
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
			var actions = from p in productionIndices.Keys
										where p.ActionCode != null
										select ConstructAction(p, p.ActionCode!, options.LineDirectivesFilePath);
			foreach (var action in actions) {
				writer.WriteLine("case {1}:{0}{2}{0}goto reduce2;", writer.NewLine, --actionIndex, action);
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
			foreach ((string name, int value) in terminals) {
				writer.WriteLine("public const int {0}={1};", name, value);
			}
			EmitSection(skeleton, writer);

			// Emit the token class, if requested.
			if (options.WantsTokenClass) {
				writer.WriteLine("public partial class Token { public int Symbol; public object? Value; }");
			}
			for (int i = 0; i < options.NamespaceNames.Length + options.ClassNames.Length - 1; ++i) {
				writer.WriteLine('}');
			}
		}

		private static string ConstructAction(Production production, ActionCode actionCode, string? lineDirectivesFilePath) {
			string code = actionCode.Code.Replace("$$.", $"(({production.Lhs.TypeName})reductionValue_).");
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
						$"(({typeName})reductionValue_{part[(j + 1)..]})" :
						$"reductionValue_{part[(j + 1)..]}";
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
					Console.Write("warning: type name not specified; using object");
					Console.WriteLine(production.LineNumber == 0 ? "" : $" in line {production.LineNumber}");
					typeName = "object";
				}
				int stackIndex = production.Rhs.Count - symbolIndex;
				string s = $"(({typeName})(stack_[stack_.Count - {stackIndex}].Value))";
				parts[i] = s + part[j..];
			}

			code = string.Join("", parts);
			if (lineDirectivesFilePath != null) {
				code = string.Format("#line {1} \"{2}\"{0}{3}{0}#line default{0}",
					Environment.NewLine, actionCode.LineNumber, lineDirectivesFilePath, code);
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
					string? lhsTypeName = production.Lhs.TypeName;
					string? rhsTypeName = production.Rhs.Count == 0 ? null : production.Rhs[0].TypeName;
					if (lhsTypeName != rhsTypeName) {
						Console.Write("warning: default action type mismatch; assigning '{0}' from '{1}'", lhsTypeName, rhsTypeName);
						Console.WriteLine(production.LineNumber == 0 ? "" : $" in line {production.LineNumber}");
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
									select (Row: r, Column: i, Target: g.TargetStateIndex);
			if (!goTos.Any()) {
				return "";
			}
			int goToRowCount = goTos.Max(g => g.Row) + 1;
			var goToArray = new int[goToRowCount, nonTerminalIndices.Count];
			foreach (var (Row, Column, Target) in goTos) {
				goToArray[Row, Column] = Target;
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

		private static ApplicationException MakeMalformedSubstitutionException(Production production) => new("malformed substitution in " + production);
	}
}
