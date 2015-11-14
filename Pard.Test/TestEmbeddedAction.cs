using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class TestEmbeddedAction
    {
        [TestMethod]
        public void ParseValidTokenStream()
        {
            var parser = new ParserEmbeddedAction("ie");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<E1><E2><iE1E2e>", parser.Result.ToString());
        }
    }

    public partial class ParserEmbeddedAction
    {
        public readonly StringBuilder Result = new StringBuilder();

        public ParserEmbeddedAction(string tokenStream) : this(new Scanner(tokenStream)) { }
    }
}
