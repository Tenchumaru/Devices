using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test {
	[TestClass]
	public class TestEpsilon {
		[TestMethod]
		public void ParseEmptyTokenStreamx() {
			var parser = new ParserEpsilonx("");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseNonemptyTokenStreamx() {
			var parser = new ParserEpsilonx("aaa");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<><a><a><a>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseEmptyTokenStreamy() {
			var parser = new ParserEpsilony("");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseNonemptyTokenStreamy() {
			var parser = new ParserEpsilony("aaa");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<><a><a><a>", parser.Result.ToString());
		}
	}

	public partial class ParserEpsilonx {
		public readonly StringBuilder Result = new StringBuilder();

		public ParserEpsilonx(string tokenStream) : this(new Scanner(tokenStream)) { }
	}

	public partial class ParserEpsilony {
		public readonly StringBuilder Result = new StringBuilder();

		public ParserEpsilony(string tokenStream) : this(new Scanner(tokenStream)) { }
	}
}
