using static Lad.Options;

namespace Lad {
	public partial class RegularExpressionParser {
		public Nfa Result => result;
		private Nfa result = new(new EpsilonSymbol());
		private readonly Parameters parameters;

		public RegularExpressionParser(RegularExpressionScanner scanner, Parameters parameters) : this(scanner) {
			this.parameters = parameters;
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

		public class Parameters {
			public Nfa AnyNfa => anyNfa.Clone();
			public Nfa NewLineNfa => newLineNfa.Clone();
			public Nfa NotNewLineNfa => notNewLineNfa.Clone();
			private static readonly Nfa posixNewLineNfa = new(new SimpleSymbol('\n'));
			private static readonly Nfa windowsNewLineNfa = new Nfa(new SimpleSymbol('\r')) + posixNewLineNfa.Clone();
			private static readonly Nfa eitherNewLineNfa = posixNewLineNfa.Clone() | windowsNewLineNfa.Clone();
			private static readonly Nfa posixNotNewLineNfa = new(~new RangeSymbol('\n'));
			private static readonly Nfa windowsNotNewLineNfa = MakeWindowsNotNewLineNfa();
			private static readonly Nfa eitherNotNewLineNfa = MakeEitherNotNewLineNfa();
			private readonly Nfa anyNfa;
			private readonly Dictionary<string, Nfa> namedExpressions;
			private readonly Nfa newLineNfa;
			private readonly Nfa notNewLineNfa;

			public Parameters(Dictionary<string, Nfa> namedExpressions, bool dotIncludesNewLineNfa, NewLineOption newLineOption) {
				this.namedExpressions = namedExpressions;
				newLineNfa = newLineOption == NewLineOption.POSIX ?
					posixNewLineNfa : newLineOption == NewLineOption.Windows ?
					windowsNewLineNfa : eitherNewLineNfa;
				notNewLineNfa = newLineOption == NewLineOption.POSIX ?
					posixNotNewLineNfa : newLineOption == NewLineOption.Windows ?
					windowsNotNewLineNfa : eitherNotNewLineNfa;
				anyNfa = dotIncludesNewLineNfa ? new Nfa(AnySymbol.Value) : notNewLineNfa;
			}

			public Nfa? FindNamedExpression(string name) {
				if (namedExpressions.TryGetValue(name, out Nfa? result)) {
					return result.Clone();
				} else {
					Console.Error.WriteLine($"cannot find named expression '{name}'");
					return null;
				}
			}

			private static Nfa MakeWindowsNotNewLineNfa() {
				// The complement of a Windows new line is a symbol that is either not a carriage return or a carriage return that is not
				// followed by a line feed.
				Nfa notCarriageReturn = new(~new RangeSymbol('\r'));
				var carriageReturnNotFollowedByLineFeed = new Nfa(new SimpleSymbol('\r')) / new Nfa(~new RangeSymbol('\n'));
				return notCarriageReturn | carriageReturnNotFollowedByLineFeed;
			}

			private static Nfa MakeEitherNotNewLineNfa() {
				// The complement of the disjunction of a POSIX new line and a Windows new line is a symbol that is either neither a carriage
				// return nor a line feed or a carriage return that is not followed by a line feed.
				Nfa neitherCarriageReturnNorLineFeed = new(~(new RangeSymbol('\r') + new RangeSymbol('\n')));
				var carriageReturnNotFollowedByLineFeed = new Nfa(new SimpleSymbol('\r')) / new Nfa(~new RangeSymbol('\n'));
				return neitherCarriageReturnNorLineFeed | carriageReturnNotFollowedByLineFeed;
			}
		}
	}
}
