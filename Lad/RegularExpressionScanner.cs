using System.Text;

namespace Lad {
	public class RegularExpressionScanner {
		public static readonly Dictionary<char, char> knownEscapes = new() {
			{'a', '\a' },
			{'b', '\b' },
			{'f', '\f' },
			{'n', '\n' },
			{'r', '\r' },
			{'t', '\t' },
			{'v', '\v' },
		};
		private readonly HashSet<char> specialCharacters = new() { '$', '(', ')', '*', '+', '.', '/', '?', '[', ']', '^', '{', '|', '}' };
		private readonly string value;
		private int index = -1;
		private bool isInCount;
		private bool isInRange;

		public RegularExpressionScanner(string value) {
			this.value = value;
		}

		public Token Read() {
			++index;
			if (index >= value.Length) {
				return new Token { Symbol = -1 };
			}
			char ch = value[index];
			if (ch == '\\') {
				++index;
				if (index >= value.Length) {
					throw new InvalidOperationException("incomplete escape");
				} else if (value[index] == 'N' && !isInRange) {
					return new Token { Symbol = RegularExpressionParser.Symbol, Value = -1 };
				} else if (knownEscapes.TryGetValue(value[index], out ch)) {
					return new Token { Symbol = RegularExpressionParser.Symbol, Value = ch };
				}
				return new Token { Symbol = RegularExpressionParser.Symbol, Value = value[index] };
			} else if (ch == ']') {
				isInRange = false;
			} else if (isInRange) {
				if (ch == '-') {
					return new Token { Symbol = ch };
				} else {
					return new Token { Symbol = RegularExpressionParser.Symbol, Value = ch };
				}
			} else if (ch == '}') {
				isInCount = false;
			} else if (isInCount) {
				if (char.IsDigit(ch)) {
					StringBuilder sb = new();
					sb.Append(ch);
					while (index + 1 < value.Length && char.IsDigit(value[index + 1])) {
						++index;
						sb.Append(value[index]);
					}
					return new Token { Symbol = RegularExpressionParser.Number, Value = int.Parse(sb.ToString()) };
				} else if (char.IsLetter(ch)) {
					StringBuilder sb = new();
					while (index < value.Length && value[index] != '}') {
						sb.Append(value[index]);
						++index;
					}
					--index;
					return new Token { Symbol = RegularExpressionParser.Identifier, Value = sb.ToString() };
				}
			} else if (!specialCharacters.Contains(ch)) {
				return new Token { Symbol = RegularExpressionParser.Symbol, Value = ch };
			} else if (ch == '{') {
				isInCount = true;
				if (index + 1 < value.Length && char.IsLetter(value[index + 1])) {
					return new Token { Symbol = RegularExpressionParser.NE };
				}
			} else if (ch == '[') {
				isInRange = true;
				if (index + 1 < value.Length && value[index + 1] == '^') {
					++index;
					return new Token { Symbol = RegularExpressionParser.OSBC };
				}
			}
			return new Token { Symbol = ch };
		}
	}

	public class Token {
		public int Symbol { get; set; }
		public object? Value { get; set; }
	}
}
