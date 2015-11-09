using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class TestEpsilon
    {
        [TestMethod]
        public void ParseValidTokenStream()
        {
            var parser = new ParserEpsilon("ie");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<E1><E2><iE1E2e>", parser.Result.ToString());
        }
    }

    public partial class ParserEpsilon
    {
        public readonly StringBuilder Result = new StringBuilder();

        public ParserEpsilon(string tokenStream) : this(new Scanner(tokenStream)) { }
    }
}
