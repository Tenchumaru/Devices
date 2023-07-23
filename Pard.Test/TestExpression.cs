using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test {
	[TestClass]
	public class TestExpression {
		[TestMethod]
		public void ParseClassx() {
			var parser = new ParserExpressionx("[\r\n]");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<range -> Symbol><range -> Symbol><class -> class range><simple -> [class]><rx -> choice>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseConcatx() {
			var parser = new ParserExpressionx("abc");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<simple -> Symbol><simple -> Symbol><concat -> concat kleene><simple -> Symbol><concat -> concat kleene><rx -> choice>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseClassy() {
			var parser = new ParserExpressiony("[\r\n]");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<range -> Symbol><range -> Symbol><class -> class range><simple -> [class]><rx -> choice>", parser.Result.ToString());
		}

		[TestMethod]
		public void ParseConcaty() {
			var parser = new ParserExpressiony("abc");
			Assert.IsTrue(parser.Parse());
			Assert.AreEqual("<simple -> Symbol><simple -> Symbol><concat -> concat kleene><simple -> Symbol><concat -> concat kleene><rx -> choice>", parser.Result.ToString());
		}
	}

	public partial class ParserExpressionx {
		public readonly StringBuilder Result = new StringBuilder();

		public ParserExpressionx(string tokenStream) : this(new ScannerExpression(tokenStream, Symbol)) { }
	}

	public partial class ParserExpressiony {
		public readonly StringBuilder Result = new StringBuilder();

		public ParserExpressiony(string tokenStream) : this(new ScannerExpression(tokenStream, Symbol)) { }
	}

	public class ScannerExpression : Scanner {
		private readonly int expressionSymbol;

		public ScannerExpression(string tokenStream, int expressionSymbol)
				: base(tokenStream) {
			this.tokenStream = tokenStream;
			this.expressionSymbol = expressionSymbol;
		}

		public override Token Read() {
			while (index < tokenStream.Length) {
				var symbol = tokenStream[index++];
				switch (symbol) {
					case '[':
					case ']':
						return new Token { Symbol = symbol };
					default:
						return new Token { Symbol = expressionSymbol };
				}
			}
			return new Token { Symbol = -1 };
		}
	}
}
