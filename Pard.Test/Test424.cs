using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class Test424
    {
        [TestMethod]
        public void ParseIf()
        {
            var parser = new Parser424("ia");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<a><iS>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseIfElse()
        {
            var parser = new Parser424("iaea");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<a><a><iSeS>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseIfIfElse()
        {
            var parser = new Parser424("iiaea");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<a><a><iSeS><iS>", parser.Result.ToString());
        }
    }

    public partial class Parser424
    {
        public readonly StringBuilder Result = new StringBuilder();

        public Parser424(string tokenStream) : this(new Scanner(tokenStream)) { }
    }
}
