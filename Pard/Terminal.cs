namespace Pard {
	class Terminal : Symbol {
		public readonly Grammar.Associativity Associativity;
		public readonly int Precedence;
		public readonly int Value;

		private static int lastValue = 1;

		public static readonly Terminal Epsilon = new("(epsilon)", null, Grammar.Associativity.None, 0);
		public static readonly Terminal AugmentedEnd = new("(end)", null, Grammar.Associativity.None, 0);
		public static readonly Terminal Error = new("error", null, Grammar.Associativity.None, 0);

		public Terminal(string name, string? typeName, Grammar.Associativity associativity, int precedence) : this(name, typeName, associativity, precedence, --lastValue) {
		}

		public Terminal(string name, string? typeName, Grammar.Associativity associativity, int precedence, int value) : base(name, typeName) {
			Associativity = associativity;
			Precedence = precedence;
			Value = value;
		}

		public static string FormatLiteralName(char ch) {
			return "'" + ch + "'";
		}

		public static string FormatLiteralName(string? value) {
			// TODO: perform unescaping on the value.
			if (value is null) {
				throw new ApplicationException("no value for literal");
			}
			return value.Length == 1 ? "'" + value + "'" : throw new ApplicationException($"invalid literal value '{value}'");
		}
	}
}
