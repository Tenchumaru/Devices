namespace Lad {
	public partial class RegularExpressionParser {
		public class Parameters {
			internal readonly Dictionary<string, Nfa> NamedExpressions;
			internal readonly bool DotIncludesNewLine;

			public Parameters(Dictionary<string, Nfa> namedExpressions, bool dotIncludesNewLine) {
				NamedExpressions = namedExpressions;
				DotIncludesNewLine = dotIncludesNewLine;
			}
		}

		private Nfa result = new(new EpsilonSymbol());
		private readonly Parameters parameters;

		public Nfa Result => result;

		public RegularExpressionParser(RegularExpressionScanner scanner, Parameters parameters) : this(scanner) {
			this.parameters = parameters;
		}

		private static Nfa MakeAnyNfaWithoutNewLine() {
			var withoutCarriageReturnAndLineFeedRange = new RangeSymbol('\r') + new RangeSymbol('\n');
			withoutCarriageReturnAndLineFeedRange = ~withoutCarriageReturnAndLineFeedRange;
			var carriageReturnNotFollowedByLineFeedNfa = new Nfa(new SimpleSymbol('\r')) / new Nfa(~new RangeSymbol('\n'));
			var anyWithoutNewLine = new Nfa(withoutCarriageReturnAndLineFeedRange) | carriageReturnNotFollowedByLineFeedNfa;
			return anyWithoutNewLine;
		}

		private static Nfa MakeNewLineNfa() {
			var newLineNfa = new Nfa(new SimpleSymbol('\r')).Question() + new Nfa(new SimpleSymbol('\n'));
			return newLineNfa;
		}

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
			if (parameters.NamedExpressions.TryGetValue(name, out Nfa? result)) {
				return result.Clone();
			} else {
				Console.Error.WriteLine($"cannot find named expression '{name}'");
				return null;
			}
		}
	}
}
