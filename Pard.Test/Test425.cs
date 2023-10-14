using System.Text;

namespace Pard.Test {
	[TestClass]
	public class Test425 {
		[TestMethod]
		public void ParseValidTokenStream1x() {
			var parser = new Parser425x("ibipi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><EsubEsupE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2x() {
			var parser = new Parser425x("ibibi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><EsubE><EsubE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream3x() {
			var parser = new Parser425x("ibibipipi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><id><id><EsupE><EsubEsupE><EsubE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream4x() {
			var parser = new Parser425x("ib{ibipi}pi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><id><EsubEsupE><{E}><id><EsubEsupE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream1y() {
			var parser = new Parser425y("ibipi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><EsubEsupE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2y() {
			var parser = new Parser425y("ibibi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><EsubE><EsubE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream3y() {
			var parser = new Parser425y("ibibipipi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><id><id><EsupE><EsubEsupE><EsubE>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream4y() {
			var parser = new Parser425y("ib{ibipi}pi");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<id><id><id><id><EsubEsupE><{E}><id><EsubEsupE>", parser.Result.ToString());
		}
	}

	public partial class Parser425x {
		public readonly StringBuilder Result = new();

		public Parser425x(string tokenStream) : this(new Scanner425(tokenStream, sub, sup, id)) { }
	}

	public partial class Parser425y {
		public readonly StringBuilder Result = new();

		public Parser425y(string tokenStream) : this(new Scanner425(tokenStream, sub, sup, id)) { }
	}

	public class Scanner425 : Scanner {
		private readonly int b;
		private readonly int p;
		private readonly int i;

		public Scanner425(string tokenStream, int b, int p, int i)
				: base(tokenStream) {
			this.tokenStream = tokenStream;
			this.b = b;
			this.p = p;
			this.i = i;
		}

		public override Token Read() {
			while (index < tokenStream.Length) {
				var symbol = tokenStream[index++];
				return symbol switch {
					'b' => new Token { Symbol = b },
					'p' => new Token { Symbol = p },
					'i' => new Token { Symbol = i },
					_ => new Token { Symbol = symbol },
				};
			}
			return new Token { Symbol = -1 };
		}
	}
}
