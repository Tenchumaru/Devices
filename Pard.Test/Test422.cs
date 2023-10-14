namespace Pard.Test {
	[TestClass]
	public class Test422 {
		[TestMethod]
		public void ParseValidTokenStream1x() {
			var parser = new Parser422x("1+2*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(7, parser.Result);
		}

		[TestMethod]
		public void ParseValidTokenStream2x() {
			var parser = new Parser422x("(1+2)*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(9, parser.Result);
		}

		[TestMethod]
		public void ParseValidTokenStream1y() {
			var parser = new Parser422y("1+2*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(7, parser.Result);
		}

		[TestMethod]
		public void ParseValidTokenStream2y() {
			var parser = new Parser422y("(1+2)*3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual(9, parser.Result);
		}
	}

	public partial class Parser422x {
		public int Result;

		public Parser422x(string tokenStream) : this(new Scanner(tokenStream, Parser433x.id)) { }
	}

	public partial class Parser422y {
		public int Result;

		public Parser422y(string tokenStream) : this(new Scanner(tokenStream, Parser433y.id)) { }
	}
}
