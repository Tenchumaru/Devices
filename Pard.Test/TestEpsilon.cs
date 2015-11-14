using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class TestEpsilon
    {
        [TestMethod]
        public void ParseEmptyTokenStream()
        {
            var parser = new ParserEpsilon("");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseNonemptyTokenStream()
        {
            var parser = new ParserEpsilon("aaa");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<><a><a><a>", parser.Result.ToString());
        }
    }

    public partial class ParserEpsilon
    {
        public readonly StringBuilder Result = new StringBuilder();

        public ParserEpsilon(string tokenStream) : this(new Scanner(tokenStream)) { }
    }
}
