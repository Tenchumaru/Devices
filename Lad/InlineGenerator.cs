using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lad {
	internal class InlineGenerator : GeneratorBase, IGenerator {
		private string? defaultCode;
		private string? methodDeclarationText;
		private string? classDeclarationText;
		private string[] namespaceNames = Array.Empty<string>();

		public InlineGenerator(Options options) : base(options) { }

		protected override void WriteFooter(StringWriter writer) {
			if (defaultCode is not null) {
				writer.WriteLine("default:");
				writer.WriteLine(defaultCode);
			}
			writer.Write(bones[2]);
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
			writer.WriteLine($"{methodDeclarationText}{{");
		}

		protected override (IEnumerable<KeyValuePair<Nfa, int>>? rules, IEnumerable<string>? codes) ProcessInput(string text) {
			bool foundError = false;
			Dictionary<Nfa, int> rules = new();
			List<string> codes = new();
			CompilationUnitSyntax root = CSharpSyntaxTree.ParseText(text).GetCompilationUnitRoot();
			var firstSwitch = root.DescendantNodes().OfType<SwitchStatementSyntax>().FirstOrDefault();
			if (firstSwitch == null) {
				Console.Error.WriteLine("Cannot find switch statement");
				return default;
			}
			var classDeclaration = firstSwitch.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
			if (classDeclaration == null) {
				Console.Error.WriteLine("No enclosing class");
				return default;
			}
			var methodDeclaration = firstSwitch.Ancestors().OfType<MethodDeclarationSyntax>().First();
			methodDeclarationText = $"{methodDeclaration.Modifiers} {methodDeclaration.ReturnType} {methodDeclaration.Identifier}()";
			classDeclarationText = $"{classDeclaration.Modifiers} class {classDeclaration.Identifier.Value}";
			namespaceNames = classDeclaration.Ancestors().OfType<NamespaceDeclarationSyntax>().Select(n => n.Name.ToString()).Reverse().ToArray();
			var q = from a in firstSwitch.Ancestors().OfType<ClassDeclarationSyntax>()
							from f in a.DescendantNodes().OfType<FieldDeclarationSyntax>()
							from v in f.Declaration.Variables
							let i = v.Initializer
							where i is not null
							select (v.Identifier.ToString(), i.Value.ToString());
			foreach ((string name, string rx) in q) {
				RegularExpressionParser parser = new(new RegularExpressionScanner(rx[1..^1]), namedExpressions);
				if (parser.Parse()) {
					namedExpressions.Add(name, parser.Result);
				} else {
					Console.Error.WriteLine($"failed to parse named regular expression");
					foundError = true;
				}
			}
			Dictionary<string, int> labelTexts = new();
			int? defaultIndex = null;
			foreach (var switchSection in firstSwitch.Sections) {
				foreach (var switchLabel in switchSection.Labels) {
					if (switchLabel is CaseSwitchLabelSyntax caseSwitchLabel) {
						var labelText = caseSwitchLabel.Value.ToString();
						labelTexts.Add(labelText, codes.Count);
						RegularExpressionParser parser = new(new RegularExpressionScanner(labelText[1..^1]), namedExpressions);
						if (parser.Parse()) {
							rules.Add(parser.Result, codes.Count);
						} else {
							Console.Error.WriteLine($"failed to parse regular expression");
							foundError = true;
						}
					} else if (switchLabel is DefaultSwitchLabelSyntax) {
						defaultIndex = codes.Count;
					}
				}
				codes.Add(switchSection.Statements.ToFullString());
			}
			var r = from g in firstSwitch.DescendantNodes().OfType<GotoStatementSyntax>()
							where g.Expression is not null && labelTexts.ContainsKey(g.Expression.ToString())
							select g;
			foreach (var item in firstSwitch.DescendantNodes().OfType<GotoStatementSyntax>()) {
				var expression = item.Expression?.ToString();
				if (expression is not null && labelTexts.ContainsKey(expression)) {
					codes = codes.Select(s => s.Replace(item.ToString(), $"goto case {labelTexts[expression] + 1};")).ToList();
				}
				var s = item.ToString();
				var span = item.Span;
			}
			if (defaultIndex.HasValue) {
				defaultCode = codes[defaultIndex.Value];
			}
			return foundError ? default : (rules, codes);
		}
	}
}
