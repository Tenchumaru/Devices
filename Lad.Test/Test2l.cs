namespace Lad.Test {
	[TestClass]
	public partial class Test2l {
		[TestMethod]
		public void Doit() {
			// Assemble
			reader = new StringReader("xor r12 $000f ; remark\r\nnop pc");

			// Act
			Token[] a = Enumerable.Range(0, int.MaxValue).Select(_ => Read()).TakeWhile(t => t != null).ToArray()!;

			// Assert
			Assert.AreEqual(6, a.Length);
			Assert.AreEqual(OP, a[0].Symbol);
			Assert.AreEqual(12 << 23, a[0].Value);
			Assert.AreEqual(REG, a[1].Symbol);
			Assert.AreEqual(12, a[1].Value);
			Assert.AreEqual(VALUE, a[2].Symbol);
			Assert.AreEqual(15, a[2].Value);
			Assert.AreEqual('\n', a[3].Symbol);
			Assert.IsNull(a[3].Value);
			Assert.AreEqual(NOP, a[4].Symbol);
			Assert.IsNull(a[4].Value);
			Assert.AreEqual(REG, a[5].Symbol);
			Assert.AreEqual(pc_index, a[5].Value);
		}

		private const int LDI = 1;
		private const int XORIH = 2;
		private const int LD = 3;
		private const int ST = 4;
		private const int SHL = 5;
		private const int SOP = 6;
		private const int VALUE = 7;
		private const int CX = 8;
		private const int AND = 9;
		private const int ASR = 10;
		private const int EQ = 11;
		private const int GE = 12;
		private const int ID = 13;
		private const int INT = 14;
		private const int LE = 15;
		private const int LSR = 16;
		private const int NE = 17;
		private const int NOP = 18;
		private const int OP = 19;
		private const int OR = 20;
		private const int REG = 21;
		private const int SET = 22;
		private TextReader reader = new StringReader("");
		private readonly int pc_index = 9;

#if DEBUG
		static Token DBOUT(object? value, string tokenValue, int symbol) {
			Console.WriteLine($" '{tokenValue}':{symbol}");
			return new Token { Symbol = symbol, Value = value };
		}
#else
		Token DBOUT(object? value, string tokenValue, int symbol) => new Token { Symbol = symbol, Value = value };
#endif
	}
}
