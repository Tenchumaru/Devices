using System.Text;

namespace Pard.Test {
	[TestClass]
	public class TestAB {
		[TestMethod]
		public void ParseValidTokenStream1x() {
			var parser = new ParserABx("aaabbb");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<empty><aSb><aSb><aSb>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2x() {
			var parser = new ParserABx("");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<empty>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream1y() {
			var parser = new ParserABy("aaabbb");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<empty><aSb><aSb><aSb>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2y() {
			var parser = new ParserABy("");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<empty>", parser.Result.ToString());
		}
	}

	public partial class ParserABx {
		public readonly StringBuilder Result = new();

		public ParserABx(string tokenStream) : this(new Scanner(tokenStream)) { }
	}

	public partial class ParserABy {
		public readonly StringBuilder Result = new();

		public ParserABy(string tokenStream) : this(new Scanner(tokenStream)) { }
	}
}
