namespace Pard.Test {
	public class Scanner {
		protected string tokenStream;
		protected int index = 0;
		protected int id;

		public Scanner(string tokenStream, int id = 0) {
			this.tokenStream = tokenStream;
			this.id = id;
		}

		public virtual Token Read() {
			while (index < tokenStream.Length) {
				int symbol = int.TryParse(tokenStream.AsSpan(index, 1), out int value) && id != 0 ? id : tokenStream[index];
				++index;
				return new Token { Symbol = symbol, Value = value };
			}
			return new Token { Symbol = -1 };
		}
	}

	public class Token {
		public int Symbol { get; set; }
		public object? Value { get; set; }
	}
}
