using System.Text;

namespace Pard.Test {
	[TestClass]
	public class Test439 {
		[TestMethod]
		public void ParseValidTokenStream1x() {
			var parser = new Parser439x("1=3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<L->id><L->id><R->L><S->L=R>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2x() {
			var parser = new Parser439x("*3=2");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<L->id><R->L><L->*R><L->id><R->L><S->L=R>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream1y() {
			var parser = new Parser439y("1=3");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<L->id><L->id><R->L><S->L=R>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStream2y() {
			var parser = new Parser439y("*3=2");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<L->id><R->L><L->*R><L->id><R->L><S->L=R>", parser.Result.ToString());
		}
	}

	public partial class Parser439x {
		public readonly StringBuilder Result = new();

		public Parser439x(string tokenStream) : this(new Scanner(tokenStream, id)) { }
	}

	public partial class Parser439y {
		public readonly StringBuilder Result = new();

		public Parser439y(string tokenStream) : this(new Scanner(tokenStream, id)) { }
	}
}
