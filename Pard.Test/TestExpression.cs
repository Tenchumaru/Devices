using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class TestExpression
    {
        [TestMethod]
        public void ParseClass()
        {
            var parser = new ParserExpression("[\r\n]");
            Assert.IsTrue(parser.ParseAndDump());
            Assert.AreEqual("<range -> Symbol><range -> Symbol><class -> class range><simple -> [class]><rx -> choice>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseConcat()
        {
            var parser = new ParserExpression("abc");
            Assert.IsTrue(parser.ParseAndDump());
            Assert.AreEqual("<simple -> Symbol><simple -> Symbol><concat -> concat kleene><simple -> Symbol><concat -> concat kleene><rx -> choice>", parser.Result.ToString());
        }
    }

    public partial class ParserExpression
    {
        public readonly StringBuilder Result = new StringBuilder();

        public ParserExpression(string tokenStream) : this(new ScannerExpression(tokenStream)) { }

        internal bool ParseAndDump()
        {
            if(!Parse())
            {
                Console.WriteLine(Result);
                return false;
            }
            return true;
        }
    }

    public class ScannerExpression : Scanner
    {
        public ScannerExpression(string tokenStream)
            : base(tokenStream)
        {
            this.tokenStream = tokenStream;
        }

        public override Token Read()
        {
            while(index < tokenStream.Length)
            {
                var symbol = tokenStream[index++];
                switch(symbol)
                {
                case '[':
                case ']':
                    return new Token { Symbol = symbol };
                default:
                    return new Token { Symbol = ParserExpression.Symbol };
                }
            }
            return new Token { Symbol = -1 };
        }
    }
}
