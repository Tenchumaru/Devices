﻿using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lad {
	public class InlineGenerator : GeneratorBase, IGenerator {
		private string[] usingDirectives = Array.Empty<string>();
		private readonly Regex wsRx = new(@"\s", RegexOptions.Compiled);

		public InlineGenerator(Options options) : base(options) { }

		protected override void WriteActions(int index, StateMachine stateMachine, StringWriter writer) {
			writer.WriteLine($"actionMap_[{index}].TryGetValue(longest.Key,out string?pattern);");
			writer.WriteLine($"var rv={stateMachine.MethodName}(pattern,tokenValue);");
			writer.WriteLine("if(rv!=default)return rv;");
		}

		protected override void WriteFooter(StringWriter writer) { }

		protected override void WriteHeader(StringWriter writer) {
			foreach (string usingDirective in usingDirectives) {
				writer.WriteLine(usingDirective);
			}
		}

		protected override IEnumerable<StateMachine>? ProcessInput(string text) {
			CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
			var q = from u in root.DescendantNodes().OfType<UsingDirectiveSyntax>()
							let s = u.ToString()
							let t = wsRx.Replace(s, "")
							where t != "usingSystem.Linq;" && t != "usingSystem.Collections.Generic;"
							select s;
			usingDirectives = q.ToArray();
			ClassDeclarationSyntax? classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
			if (classDeclaration == null) {
				Console.Error.WriteLine("no class in file");
				return default;
			}
			namespaceNames = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Reverse().ToArray();
			var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
			if (!methodDeclarations.Any()) {
				Console.Error.WriteLine("no methods in class");
				return default;
			}
			classDeclaration = methodDeclarations.First().Parent as ClassDeclarationSyntax;
			if (classDeclaration == null) {
				Console.Error.WriteLine("methods are not in a class");
				return default;
			}
			if (!methodDeclarations.Skip(1).All((m) => m.Parent == classDeclaration)) {
				Console.Error.WriteLine("not all methods are in the same class");
				return default;
			}
			var r = from f in classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
							from v in f.Declaration.Variables
							let i = v.Initializer
							where i is not null
							select (v.Identifier.ToString(), i.Value.ToString());
			bool foundError = false;
			foreach ((string name, string rx) in r) {
				Nfa? nfa = ParseSyntaxValue(rx, $" named '{name}'");
				if (nfa is not null) {
					namedExpressions.Add(name, nfa);
				} else {
					foundError = true;
				}
			}
			List<ClassDeclarationSyntax> classDeclarations = new();
			do {
				classDeclarations.Insert(0, classDeclaration);
				classDeclaration = classDeclaration.Parent as ClassDeclarationSyntax;
			} while (classDeclaration != null);
			classAccesses = classDeclarations.Select(c => c.Modifiers.ToString()).ToArray();
			classNames = classDeclarations.Select(c => c.Identifier.Value!.ToString()!).ToArray();
			StateMachine?[] stateMachines = methodDeclarations.Select(ProcessMethod).ToArray();
			if (foundError || stateMachines.Any(s => s is null)) {
				return default;
			}
			return stateMachines!;
		}

		private Nfa? ParseSyntaxValue(string value, string? context = "") {
			if (value[0] == '@') {
				// Convert a literal string into a standard string unless the literal parsing specifier is present.
				if (value[2] == '$') {
					value = value[1..];
				} else {
					var sb = new StringBuilder(value[1..]);
					for (int i = value.IndexOf("\"\"", 2); i >= 0; i = value.IndexOf("\"\"", i + 2)) {
						sb[i - 1] = '\\';
					}
					return ParseSyntaxValue(sb.ToString(), context);
				}
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
			string methodName = methodDeclaration.Identifier.ToString();
			string methodDeclarationText = $"{methodDeclaration.Modifiers} {methodDeclaration.ReturnType} {methodName}()";
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
			return foundError ? default : new StateMachine(methodDeclarationText, methodName, labelTexts, rules, codes, defaultIndex);
		}
	}
}
