using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lad {
	internal class InlineGenerator : GeneratorBase, IGenerator {
		private string? classDeclarationText;
		private string[] namespaceNames = Array.Empty<string>();

		public InlineGenerator(Options options) : base(options) { }

		protected override void WriteFooter(StringWriter writer) {
			writer.Write(bones[3]);
			writer.WriteLine(new string('}', namespaceNames.Length));
		}

		protected override void WriteHeader(StringWriter writer) {
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
			var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
			if (classDeclaration == null) {
				Console.Error.WriteLine("No class in file");
				return default;
			}
			classDeclarationText = $"{classDeclaration.Modifiers} class {classDeclaration.Identifier.Value}";
			namespaceNames = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Reverse().ToArray();
			var methodDeclarations = classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>().ToArray();
			if (!methodDeclarations.Any()) {
				Console.Error.WriteLine("No methods in class");
				return default;
			}
			var q = from f in classDeclaration.DescendantNodes().OfType<FieldDeclarationSyntax>()
							from v in f.Declaration.Variables
							let i = v.Initializer
							where i is not null
							select (v.Identifier.ToString(), i.Value.ToString());
			bool foundError = false;
			foreach ((string name, string rx) in q) {
				RegularExpressionParser parser = new(new RegularExpressionScanner(rx[1..^1]), namedExpressions);
				if (parser.Parse()) {
					namedExpressions.Add(name, parser.Result);
				} else {
					Console.Error.WriteLine($"failed to parse named regular expression '{name}'");
					foundError = true;
				}
			}
			StateMachine?[] stateMachines = methodDeclarations.Select(ProcessMethod).ToArray();
			if (foundError || stateMachines.Any(s => s is null)) {
				return default;
			}
			return stateMachines!;
		}

		private StateMachine? ProcessMethod(MethodDeclarationSyntax methodDeclaration) {
			bool foundError = false;
			Dictionary<Nfa, int> rules = new();
			List<string> codes = new();
			var firstSwitch = methodDeclaration.DescendantNodes().OfType<SwitchStatementSyntax>().FirstOrDefault();
			if (firstSwitch == null) {
				Console.Error.WriteLine("Cannot find switch statement");
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
						string value = labelText[1..^1];
						RegularExpressionParser parser = new(new RegularExpressionScanner(value), namedExpressions);
						if (parser.Parse()) {
							rules.Add(parser.Result, codes.Count);
						} else {
							Console.Error.WriteLine($"failed to parse regular expression '{value}'");
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
			string? defaultCode = defaultIndex.HasValue ? codes[defaultIndex.Value] : null;
			return foundError ? default : new StateMachine(methodDeclarationText, rules, codes, defaultCode);
		}
	}
}
