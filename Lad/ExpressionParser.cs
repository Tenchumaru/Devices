using System;
using System.Collections.Generic;

namespace Lad {
	public partial class ExpressionParser {
		public bool dotIncludesNewline;
		public bool ignoringCase;
		public IDictionary<string, Nfa> namedExpressions;
		private Nfa machine;

		internal ExpressionParser(ExpressionScanner scanner, bool dotIncludesNewline, bool ignoringCase, IDictionary<string, Nfa> namedExpressions)
				: this(scanner) {
			this.dotIncludesNewline = dotIncludesNewline;
			this.ignoringCase = ignoringCase;
			this.namedExpressions = namedExpressions;
		}

		internal Nfa CreateMachine() {
			return Parse() ? machine : null;
		}

		private Nfa CloneNamedNfa(string name) {
			if (!namedExpressions.TryGetValue(name, out Nfa nfa))
				throw new Exception(string.Format("named expression \"{0}\" not defined", name));
			return new Nfa(nfa);
		}

		private RangeSymbol CreateRange(char ch) {
			if (ignoringCase && char.IsLetter(ch)) {
				var lower = char.ToLower(ch);
				var lowerRange = new RangeSymbol(lower, lower);
				var upper = char.ToUpper(ch);
				var upperRange = new RangeSymbol(upper, upper);
				return lowerRange + upperRange;
			} else
				return new RangeSymbol(ch, ch);
		}

		private RangeSymbol CreateRange(char first, char last) {
			if (ignoringCase) {
				var range = new RangeSymbol(first, last);
				var lower = new RangeSymbol(char.ToLower(first), char.ToLower(last));
				var upper = new RangeSymbol(char.ToUpper(first), char.ToUpper(last));
				return range + lower + upper;
			} else
				return new RangeSymbol(first, last);
		}
	}
}
