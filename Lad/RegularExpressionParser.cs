namespace Lad {
	public partial class RegularExpressionParser {
		private Nfa result = new(new EpsilonSymbol());
		private readonly Dictionary<string, Nfa> namedExpressions;

		public Nfa Result => result;

		public RegularExpressionParser(RegularExpressionScanner scanner, Dictionary<string, Nfa> namedExpressions) : this(scanner) => this.namedExpressions = namedExpressions;

		private static bool ValidateKleeneCount(int first, int second) {
			if (second < first) {
				Console.Error.WriteLine("second Kleene count must be at least first");
				return false;
			} else if (second < 1) {
				Console.Error.WriteLine("second Kleene count must be at least one");
				return false;
			}
			return true;
		}

		private Nfa? FindNamedExpression(string name) {
			if (namedExpressions.TryGetValue(name, out Nfa? result)) {
				return result.Clone();
			} else {
				Console.Error.WriteLine($"cannot find named expression '{name}'");
				return null;
			}
		}
	}
}
