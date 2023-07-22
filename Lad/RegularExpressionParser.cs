using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lad;

namespace Lad {
	public partial class RegularExpressionParser {
		private Nfa result = new(new EpsilonSymbol());
		private readonly Dictionary<string, Nfa> namedExpressions;

		public Nfa Result => result;

		public RegularExpressionParser(RegularExpressionScanner scanner, Dictionary<string, Nfa> namedExpressions) : this(scanner) => this.namedExpressions = namedExpressions;

		private static bool ValidateKleeneCount(int first, int? second = null) {
			if (first < 1) {
				Console.Error.WriteLine("Kleene count value must be at least 1");
				return false;
			} else if (second.HasValue) {
				if (second.Value < first) {
					Console.Error.WriteLine("Second Kleene count must be at least first");
					return false;
				}
			}
			return true;
		}

		private Nfa? FindNamedExpression(string name) {
			if (namedExpressions.TryGetValue(name, out Nfa? result)) {
				return result.Clone();
			} else {
				Console.Error.WriteLine($"Cannot find named expression '{name}'");
				return null;
			}
		}
	}
}
