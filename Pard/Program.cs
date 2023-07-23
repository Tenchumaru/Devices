using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Pard {
	class Program {
		public static readonly string Name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

		static void Main(string[] args) {
#if !DEBUG
			AppDomain.CurrentDomain.AssemblyResolve += Resolve;
#endif
			// Read the productions from the input.
			var options = new Options(args);
			IReadOnlyList<Production> productions;
			using (var reader = File.OpenText(options.InputFilePath)) {
				productions = options.GrammarInput.Read(reader, options);
			}
			if (productions == null) {
				Environment.Exit(1);
			}

			// Check the productions for undefined symbols.
			var q = from n in productions.SelectMany(p => p.Rhs).OfType<Nonterminal>()
							where !productions.Any(p => p.Lhs == n)
							select n;
			var undefinedSymbols = q.Distinct().OrderBy(n => n.Name).ToList();
			if (undefinedSymbols.Any()) {
				Console.Error.WriteLine("error: {0} undefined symbol{1}:", undefinedSymbols.Count, undefinedSymbols.Count == 1 ? "" : "s");
				foreach (var undefinedSymbol in undefinedSymbols) {
					Console.Error.WriteLine("\t{0}", undefinedSymbol);
				}
				Environment.Exit(1);
			}

			// Create the grammar from the productions.
			var grammar = new Grammar(productions);

			// Write the parser.
			var output = new CodeOutput();
			using (var writer = options.OutputFilePath != null ? new StreamWriter(options.OutputFilePath, false, Encoding.UTF8) : Console.Out) {
				output.Write(grammar.Actions, grammar.Gotos, productions, writer, options);
			}

			if (options.StateOutputFilePath != null) {
				using var writer = new StreamWriter(options.StateOutputFilePath, false, Encoding.UTF8);
				var dict = productions.ToDictionary(p => p.Index);
				var states = grammar.States.Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);
				foreach (var item in states) {
					writer.WriteLine();
					writer.WriteLine("state {0}:", item.Value);
					writer.WriteLine(item.Key.ToString(dict));
					writer.WriteLine();
					foreach (var pair in item.Key.Gotos) {
						writer.WriteLine("\t{0} -> {1}", pair.Key, states[pair.Value]);
					}
				}
			}
		}

#if !DEBUG
		static Assembly Resolve(object sender, ResolveEventArgs args) {
			var resourceName = typeof(Program).Namespace + '.' + new AssemblyName(args.Name).Name + ".dll";
			using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
			var assemblyData = new Byte[stream.Length];
			stream.Read(assemblyData, 0, assemblyData.Length);
			return Assembly.Load(assemblyData);
		}
#endif
	}
}
