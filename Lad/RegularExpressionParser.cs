﻿using static Lad.Options;

namespace Lad {
	public partial class RegularExpressionParser {
		public Nfa Result => result;
		private Nfa result = new(new EpsilonSymbol());
		private readonly Parameters parameters;

		public RegularExpressionParser(RegularExpressionScanner scanner, Parameters parameters) : this(scanner) {
			this.parameters = parameters;
		}

		private void ComposeResult(bool bol, Nfa nfa, bool eol) {
			if (bol) {
				nfa = new Nfa(BolSymbol.Value) + nfa;
			}
			if (eol) {
				nfa /= parameters.NewLineNfa;
			}
			result = nfa;
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
			private static readonly Nfa posixNewLineNfa = new(new SimpleSymbol('\n'));
			private static readonly Nfa windowsNewLineNfa = new Nfa(new SimpleSymbol('\r')) + new Nfa(new SimpleSymbol('\n'));
			private static readonly Nfa eitherNewLineNfa = posixNewLineNfa.Clone() | windowsNewLineNfa.Clone();
			private readonly Nfa anyNfa;
			private readonly Dictionary<string, Nfa> namedExpressions;
			private readonly Nfa newLineNfa;

			public Parameters(Dictionary<string, Nfa> namedExpressions, bool dotIncludesNewLineNfa, NewLineOption newLineOption) {
				this.namedExpressions = namedExpressions;
				newLineNfa = newLineOption == NewLineOption.POSIX ? posixNewLineNfa :
					newLineOption == NewLineOption.Windows ? windowsNewLineNfa : eitherNewLineNfa;
				anyNfa = dotIncludesNewLineNfa ? new Nfa(AnySymbol.Value) :
					newLineOption == NewLineOption.POSIX ? new Nfa(~new RangeSymbol('\n')) :
					newLineOption == NewLineOption.Windows ? MakeWindowsNotNewLineNfa() : MakeEitherNotNewLineNfa();
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
				// followed by a line feed, including carriage returns at EOF.
				Nfa notCarriageReturn = new(~new RangeSymbol('\r'));
				var carriageReturnNotFollowedByLineFeed = new Nfa(new SimpleSymbol('\r')) / (new Nfa(~new RangeSymbol('\n')) | new Nfa(EofSymbol.Value));
				return notCarriageReturn | carriageReturnNotFollowedByLineFeed;
			}

			private static Nfa MakeEitherNotNewLineNfa() {
				// The complement of the disjunction of a POSIX new line and a Windows new line is a symbol that is either neither a carriage
				// return nor a line feed or a carriage return that is not followed by a line feed, including carriage returns at EOF.
				Nfa neitherCarriageReturnNorLineFeed = new(~(new RangeSymbol('\r') + new RangeSymbol('\n')));
				var carriageReturnNotFollowedByLineFeed = new Nfa(new SimpleSymbol('\r')) / (new Nfa(~new RangeSymbol('\n')) | new Nfa(EofSymbol.Value));
				return neitherCarriageReturnNorLineFeed | carriageReturnNotFollowedByLineFeed;
			}
		}
	}
}
