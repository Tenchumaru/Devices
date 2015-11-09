using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class TestAB
    {
        [TestMethod]
        public void ParseValidTokenStream1()
        {
            var parser = new ParserAB("aaabbb");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<empty><aSb><aSb><aSb>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseValidTokenStream2()
        {
            var parser = new ParserAB("");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<empty>", parser.Result.ToString());
        }
    }

    public partial class ParserAB
    {
        public readonly StringBuilder Result = new StringBuilder();

        public ParserAB(string tokenStream) : this(new Scanner(tokenStream)) { }
    }
}
