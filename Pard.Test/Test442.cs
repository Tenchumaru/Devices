using System.Text;

namespace Pard.Test {
	[TestClass]
	public class Test442 {
		[TestMethod]
		public void ParseValidTokenStream1x() {
			var parser = new Parser442x("cccdcd");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<d><cC><cC><cC><d><cC><CC>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2x() {
			var parser = new Parser442x("dd");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<d><d><CC>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseInvalidTokenStream1x() {
			var parser = new Parser442x("ccd");
			Assert.IsFalse(parser.Parse());
			Assert.AreEqual("", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseInvalidTokenStream2x() {
			var parser = new Parser442x("ccdc");
			Assert.IsFalse(parser.Parse());
			Assert.AreEqual("<d><cC><cC>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseInvalidTokenStream3x() {
			var parser = new Parser442x("ddd");
			Assert.IsFalse(parser.Parse());
			Assert.AreEqual("<d>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream1y() {
			var parser = new Parser442y("cccdcd");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<d><cC><cC><cC><d><cC><CC>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2y() {
			var parser = new Parser442y("dd");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<d><d><CC>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseInvalidTokenStream1y() {
			var parser = new Parser442y("ccd");
			Assert.IsFalse(parser.Parse());
			Assert.AreEqual("", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseInvalidTokenStream2y() {
			var parser = new Parser442y("ccdc");
			Assert.IsFalse(parser.Parse());
			Assert.AreEqual("<d><cC><cC>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseInvalidTokenStream3y() {
			var parser = new Parser442y("ddd");
			Assert.IsFalse(parser.Parse());
			Assert.AreEqual("<d>", parser.Result.ToString());
		}
	}

	public partial class Parser442x {
		public readonly StringBuilder Result = new();

		public Parser442x(string tokenStream) : this(new Scanner(tokenStream)) { }
	}

	public partial class Parser442y {
		public readonly StringBuilder Result = new();

		public Parser442y(string tokenStream) : this(new Scanner(tokenStream)) { }
	}
}
