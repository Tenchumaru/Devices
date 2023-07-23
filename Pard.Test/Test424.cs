using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test {
	[TestClass]
	public class Test424 {
		[TestMethod]
		public void ParseIfx() {
			var parser = new Parser424x("ia");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<a><iS>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseIfElsex() {
			var parser = new Parser424x("iaea");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<a><a><iSeS>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseIfIfElsex() {
			var parser = new Parser424x("iiaea");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<a><a><iSeS><iS>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseIfy() {
			var parser = new Parser424y("ia");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<a><iS>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseIfElsey() {
			var parser = new Parser424y("iaea");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<a><a><iSeS>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseIfIfElsey() {
			var parser = new Parser424y("iiaea");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<a><a><iSeS><iS>", parser.Result.ToString());
		}
	}

	public partial class Parser424x {
		public readonly StringBuilder Result = new StringBuilder();

		public Parser424x(string tokenStream) : this(new Scanner(tokenStream)) { }
	}

	public partial class Parser424y {
		public readonly StringBuilder Result = new StringBuilder();

		public Parser424y(string tokenStream) : this(new Scanner(tokenStream)) { }
	}
}
