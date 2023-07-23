using System.Text;

namespace Pard {
	class Program {
		public static readonly string Name = Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]);

		static void Main(string[] args) {
			// Read the productions from the input.
			Options options = new(args);
			IReadOnlyList<Production>? productions;
			using (var reader = options.OpenReader()) {
				productions = options.GrammarInput.Read(reader);
			}
			if (productions == null) {
				Environment.Exit(1);
			}

			// Check the productions for undefined symbols.
			var q = from n in productions.SelectMany(p => p.Rhs).OfType<Nonterminal>()
							where !productions.Any(p => p.Lhs == n)
							select n;
			List<Nonterminal> undefinedSymbols = q.Distinct().OrderBy(n => n.Name).ToList();
			if (undefinedSymbols.Any()) {
				Console.Error.WriteLine("error: {0} undefined symbol{1}:", undefinedSymbols.Count, undefinedSymbols.Count == 1 ? "" : "s");
				foreach (var undefinedSymbol in undefinedSymbols) {
					Console.Error.WriteLine("\t{0}", undefinedSymbol);
				}
				Environment.Exit(1);
			}

			// Create the grammar from the productions.
			Grammar grammar = new(productions);

			// Write the parser.
			CodeOutput output = new();
			using (TextWriter writer = options.OutputFilePath != null ? new StreamWriter(options.OutputFilePath, false, Encoding.UTF8) : Console.Out) {
				output.Write(grammar.Actions, grammar.Gotos, productions, writer, options);
			}

			if (options.StateOutputFilePath != null) {
				using StreamWriter writer = new(options.StateOutputFilePath, false, Encoding.UTF8);
				Dictionary<int, Production> dict = productions.ToDictionary(p => p.Index);
				Dictionary<Item.Set, int> states = grammar.States.Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);
				foreach (KeyValuePair<Item.Set, int> item in states) {
					writer.WriteLine();
					writer.WriteLine("state {0}:", item.Value);
					writer.WriteLine(item.Key.ToString(dict));
					writer.WriteLine();
					foreach (KeyValuePair<Symbol, Item.Set> pair in item.Key.Gotos) {
						writer.WriteLine("\t{0} -> {1}", pair.Key, states[pair.Value]);
					}
				}
			}
		}
	}
}
