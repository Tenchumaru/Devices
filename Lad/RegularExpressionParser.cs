using static Lad.Options;

namespace Lad {
	public partial class RegularExpressionParser {
		public Nfa Result => result;
		private Nfa result = new(new EpsilonSymbol());
		private readonly Parameters parameters;

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

		public class Parameters {
			public Nfa NewLineNfa => newLineNfa.Clone();
			public Nfa NotNewLineNfa => notNewLineNfa.Clone();
			public readonly Dictionary<string, Nfa> NamedExpressions;
			public readonly bool DotIncludesNewLine;
			private static readonly Nfa posixNewLine = new(new SimpleSymbol('\n'));
			private static readonly Nfa windowsNewLine = new Nfa(new SimpleSymbol('\r')) + posixNewLine;
			private static readonly Nfa eitherNewLine = posixNewLine | windowsNewLine;
			private static readonly Nfa posixNotNewLine = new(~new RangeSymbol('\n'));
			private static readonly Nfa windowsNotNewLine = MakeWindowsNotNewLineNfa();
			private static readonly Nfa eitherNotNewLine = MakeEitherNotNewLineNfa();
			private readonly Nfa newLineNfa;
			private readonly Nfa notNewLineNfa;

			public Parameters(Dictionary<string, Nfa> namedExpressions, bool dotIncludesNewLine, NewLineOption newLineOption) {
				NamedExpressions = namedExpressions;
				DotIncludesNewLine = dotIncludesNewLine;
				newLineNfa = newLineOption == NewLineOption.POSIX ?
					posixNewLine : newLineOption == NewLineOption.Windows ?
					windowsNewLine : eitherNewLine;
				notNewLineNfa = newLineOption == NewLineOption.POSIX ?
					posixNotNewLine : newLineOption == NewLineOption.Windows ?
					windowsNotNewLine : eitherNotNewLine;
			}
		}
	}
}
