using System.Text;

namespace Pard.Test {
	[TestClass]
	public class TestEmbeddedAction {
		[TestMethod]
		public void ParseValidTokenStreamx() {
			var parser = new ParserEmbeddedActionx("ie");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<E1><E2><iE1E2e>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseValidTokenStreamy() {
			var parser = new ParserEmbeddedActiony("ie");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<E1><E2><iE1E2e>", parser.Result.ToString());
		}
	}

	public partial class ParserEmbeddedActionx {
		public readonly StringBuilder Result = new();

		public ParserEmbeddedActionx(string tokenStream) : this(new Scanner(tokenStream)) { }
	}

	public partial class ParserEmbeddedActiony {
		public readonly StringBuilder Result = new();

		public ParserEmbeddedActiony(string tokenStream) : this(new Scanner(tokenStream)) { }
	}
}
