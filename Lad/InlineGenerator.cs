﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lad {
	internal class InlineGenerator : GeneratorBase, IGenerator {
		private string? classDeclarationText;
		private string[] usingDirectives = Array.Empty<string>();
		private string[] namespaceNames = Array.Empty<string>();

		public InlineGenerator(Options options) : base(options) { }

		protected override void WriteFooter(StringWriter writer) {
			writer.Write(bones[3]);
			writer.WriteLine(new string('}', namespaceNames.Length));
		}

		protected override void WriteHeader(StringWriter writer) {
			foreach (string usingDirective in usingDirectives) {
				writer.WriteLine(usingDirective);
			}
			writer.Write(bones[0]);
			foreach (string namespaceName in namespaceNames) {
				writer.WriteLine($"namespace {namespaceName}{{");
			}
			writer.Write($"{classDeclarationText}{{");
			writer.Write(bones[1]);
		}

		protected override IEnumerable<StateMachine>? ProcessInput(string text) {
			Dictionary<Nfa, int> rules = new();
			List<string> codes = new();
			CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
			var usingDirectives = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
			this.usingDirectives = usingDirectives.Select(s => s.ToString()).Where(s => !s.Contains(" System.Linq;") && !s.Contains(" System.Collections.Generic;")).ToArray();
			var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
			if (classDeclaration == null) {
				Console.Error.WriteLine("no class in file");
				return default;
			}
			classDeclarationText = $"{classDeclaration.Modifiers} class {classDeclaration.Identifier.Value}";
			namespaceNames = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Reverse().ToArray();
			var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
			if (!methodDeclarations.Any()) {
				Console.Error.WriteLine("no methods in class");
				return default;
			}
			var q = from f in classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
							from v in f.Declaration.Variables
							let i = v.Initializer
							where i is not null
							select (v.Identifier.ToString(), i.Value.ToString());
			bool foundError = false;
			foreach ((string name, string rx) in q) {
				Nfa? nfa = ParseSyntaxValue(rx, $" named '{name}'");
				if (nfa is not null) {
					namedExpressions.Add(name, nfa);
				} else {
					foundError = true;
				}
			}
			StateMachine?[] stateMachines = methodDeclarations.Select(ProcessMethod).ToArray();
			if (foundError || stateMachines.Any(s => s is null)) {
				return default;
			}
			return stateMachines!;
		}

		private Nfa? ParseSyntaxValue(string value, string? context = "") {
			if (value[0] == '@') {
				Nfa nfa = new(new EpsilonSymbol());
				bool isEscaping = false;
				int index = 1;
				value = value[2..^1];
				if (!value.Any()) {
					Console.Error.WriteLine($"cannot parse empty regular expression{context}");
					return null;
				}
				foreach (char ch in value) {
					++index;
					if (isEscaping) {
						isEscaping = false;
						if (!RegularExpressionScanner.knownEscapes.TryGetValue(ch, out char escape)) {
							escape = ch;
						}
						nfa += new Nfa(new SimpleSymbol(escape));
					} else if (ch == '\\') {
						isEscaping = true;
					} else {
						nfa += new Nfa(new SimpleSymbol(ch));
					}
				}
				if (isEscaping) {
					Console.Error.WriteLine($"unterminated escape in literal in '{value}'{context}");
					return null;
				}
				return nfa;
			}
			value = value[1..^1];
			if (!value.Any()) {
				Console.Error.WriteLine($"cannot parse empty regular expression{context}");
				return null;
			}
			RegularExpressionParser parser = new(new RegularExpressionScanner(value), parameters);
			if (!parser.Parse()) {
				Console.Error.WriteLine($"cannot parse regular expression '{value}'{context}");
				return null;
			}
			if (parser.Result.CheckForEmpty()) {
				Console.Error.WriteLine($"regular expression '{value}'{context} accepts empty");
				return null;
			}
			return parser.Result;
		}

		private StateMachine? ProcessMethod(MethodDeclarationSyntax methodDeclaration) {
			bool foundError = false;
			Dictionary<Nfa, int> rules = new();
			List<string> codes = new();
			var firstSwitch = methodDeclaration.DescendantNodes().OfType<SwitchStatementSyntax>().FirstOrDefault();
			if (firstSwitch == null) {
				Console.Error.WriteLine("cannot find switch statement");
				return default;
			}
			string methodDeclarationText = $"{methodDeclaration.Modifiers} {methodDeclaration.ReturnType} {methodDeclaration.Identifier}()";
			Dictionary<string, int> labelTexts = new();
			int? defaultIndex = null;
			foreach (var switchSection in firstSwitch.Sections) {
				foreach (var switchLabel in switchSection.Labels) {
					if (switchLabel is CaseSwitchLabelSyntax caseSwitchLabel) {
						var labelText = caseSwitchLabel.Value.ToString();
						labelTexts.Add(labelText, codes.Count);
						Nfa? nfa = ParseSyntaxValue(labelText);
						if (nfa is not null) {
							rules.Add(nfa, codes.Count);
						} else {
							foundError = true;
						}
					} else if (switchLabel is DefaultSwitchLabelSyntax) {
						defaultIndex = codes.Count;
					}
				}
				codes.Add(switchSection.Statements.ToFullString());
			}
			if (!isDebug) {
				foreach (var item in firstSwitch.DescendantNodes().OfType<GotoStatementSyntax>()) {
					var expression = item.Expression?.ToString();
					if (expression is not null && labelTexts.ContainsKey(expression)) {
						codes = codes.Select(s => s.Replace(item.ToString(), $"goto case {labelTexts[expression] + 1};")).ToList();
					}
				}
			}
			return foundError ? default : new StateMachine(methodDeclarationText, rules, codes, defaultIndex);
		}
	}
}
