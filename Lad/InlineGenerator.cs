using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Lad {
	internal class InlineGenerator : GeneratorBase<int>, IGenerator {
		private string? defaultCode;
		private string? methodDeclarationText;
		private string? classDeclarationText;
		private string[] namespaceNames = Array.Empty<string>();

		public InlineGenerator(Options options) : base(options) { }

		protected override void WriteFooter(int nnamespaces, StringWriter writer) {
			if (defaultCode is not null) {
				writer.WriteLine("default:");
				writer.WriteLine(defaultCode);
			}
			writer.WriteLine("}break;}}}}");
			while (--nnamespaces >= 0) {
				writer.WriteLine('}');
			}
		}

		protected override int WriteHeader(StringWriter writer) {
			writer.WriteLine("using System.Collections.Generic;");
			writer.WriteLine("using System.Linq;");
			foreach (string namespaceName in namespaceNames) {
				writer.WriteLine($"namespace {namespaceName}{{");
			}
			writer.WriteLine($"{classDeclarationText}{{");
			writer.WriteLine("private class Reader_{");
			writer.WriteLine("internal int Position=>index;");
			writer.WriteLine("private IEnumerator<char>enumerator;");
			writer.WriteLine("private System.Text.StringBuilder buffer=new System.Text.StringBuilder();");
			writer.WriteLine("private int index=0;");
			writer.WriteLine("internal string Consume(int position){");
			writer.WriteLine("index=0;position=System.Math.Min(position,buffer.Length);if(position==0)return\"\";");
			writer.WriteLine("var s=buffer.ToString(0,position);buffer.Remove(0,position);return s;}");
			writer.WriteLine("internal int Read(){");
			writer.WriteLine("if(index<buffer.Length)return buffer[index++];");
			writer.WriteLine("if(enumerator.MoveNext()){buffer.Append(enumerator.Current);return buffer[index++];}");
			writer.WriteLine("return-1;}");
			writer.WriteLine("internal Reader_(IEnumerable<char>reader){enumerator=reader.GetEnumerator();}");
			writer.WriteLine("internal Reader_(System.IO.TextReader reader){enumerator=Enumerable.");
			writer.WriteLine("Repeat<System.Func<int>>(reader.Read,int.MaxValue).Select(f=>f()).TakeWhile(v=>v>=0).Cast<char>().GetEnumerator();}");
			writer.WriteLine("}private Reader_ reader_;");
			writer.WriteLine($"{methodDeclarationText}{{");
			return namespaceNames.Length;
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
