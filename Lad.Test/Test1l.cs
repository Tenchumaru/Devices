namespace Lad.Test {
	[TestClass]
	public partial class Test1l {
		[TestMethod]
		public void Doit() {
			// Assemble
			reader = new StringReader("cp r3 -1 ; remark\r\n33 /*-*/\n");

			// Act
			Token[] a = Enumerable.Range(0, int.MaxValue).Select(_ => Read()).TakeWhile(t => t != null).ToArray()!;

			// Assert
			Assert.AreEqual(4, a.Length);
			Assert.AreEqual(Token.Copy, a[0]);
			Assert.AreEqual(Parser.Register, a[1].Symbol);
			Assert.AreEqual(3, a[1].Value);
			Assert.AreEqual(Token.NegativeOne, a[2]);
			Assert.AreEqual(Parser.Number, a[3].Symbol);
			Assert.AreEqual(33, a[3].Value);
		}

		TextReader reader = new StringReader("");

		void IgnoreComment() {
			var r = reader_!;
			r.Read();
			r.Read();
			r.Read();
			r.Consume(r.Position);
		}

		public class Token {
			internal int Symbol;
			internal object? Value;

			internal static readonly Token Count = new() { Symbol = 1 };
			internal static readonly Token Amplify = new() { Symbol = 2 };
			internal static readonly Token Attenuate = new() { Symbol = 3 };
			internal static readonly Token Clip = new() { Symbol = 4 };
			internal static readonly Token Hpf = new() { Symbol = 5 };
			internal static readonly Token Spread = new() { Symbol = 6 };
			internal static readonly Token Train = new() { Symbol = 7 };
			internal static readonly Token Buy = new() { Symbol = 8 };
			internal static readonly Token March = new() { Symbol = 9 };
			internal static readonly Token Support = new() { Symbol = 10 };
			internal static readonly Token Investigate = new() { Symbol = 11 };
			internal static readonly Token Copy = new() { Symbol = 12 };
			internal static readonly Token Add = new() { Symbol = 13 };
			internal static readonly Token Subtract = new() { Symbol = 14 };
			internal static readonly Token ExclusiveOr = new() { Symbol = 15 };
			internal static readonly Token Accumulator = new() { Symbol = 16 };
			internal static readonly Token Zero = new() { Symbol = 17 };
			internal static readonly Token My = new() { Symbol = 18 };
			internal static readonly Token Army = new() { Symbol = 19 };
			internal static readonly Token Enemy = new() { Symbol = 20 };
			internal static readonly Token Camp = new() { Symbol = 21 };
			internal static readonly Token Not = new() { Symbol = 22 };
			internal static readonly Token Negative = new() { Symbol = 23 };
			internal static readonly Token One = new() { Symbol = 24 };
			internal static readonly Token NegativeOne = new() { Symbol = 25 };
		}

		class Parser {
			internal const int Number = 1;
			internal const int Register = 2;
		}
	}
}
