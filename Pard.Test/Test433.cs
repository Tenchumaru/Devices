namespace Pard.Test {
	[TestClass]
	public class Test433 {
		[TestMethod]
		public void ParseValidTokenStream1x() {
			var parser = new Parser433x("1+2*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(7, parser.Result);
		}

		[TestMethod]
		public void ParseValidTokenStream2x() {
			var parser = new Parser433x("(1+2)*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(9, parser.Result);
		}

		[TestMethod]
		public void ParseValidTokenStream1y() {
			var parser = new Parser433y("1+2*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(7, parser.Result);
		}

		[TestMethod]
		public void ParseValidTokenStream2y() {
			var parser = new Parser433y("(1+2)*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(9, parser.Result);
		}
	}

	public partial class Parser433x {
		public int Result;

		public Parser433x(string tokenStream) : this(new Scanner(tokenStream, id)) { }
	}

	public partial class Parser433y {
		public int Result;

		public Parser433y(string tokenStream) : this(new Scanner(tokenStream, id)) { }
	}
}
