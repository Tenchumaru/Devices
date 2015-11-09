using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class Test442
    {
        [TestMethod]
        public void ParseValidTokenStream1()
        {
            var parser = new Parser442("cccdcd");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<d><cC><cC><cC><d><cC><CC>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseValidTokenStream2()
        {
            var parser = new Parser442("dd");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<d><d><CC>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseInvalidTokenStream1()
        {
            var parser = new Parser442("ccd");
            Assert.IsFalse(parser.Parse());
            Assert.AreEqual("", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseInvalidTokenStream2()
        {
            var parser = new Parser442("ccdc");
            Assert.IsFalse(parser.Parse());
            Assert.AreEqual("<d><cC><cC>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseInvalidTokenStream3()
        {
            var parser = new Parser442("ddd");
            Assert.IsFalse(parser.Parse());
            Assert.AreEqual("<d>", parser.Result.ToString());
        }
    }

    public partial class Parser442
    {
        public readonly StringBuilder Result = new StringBuilder();

        public Parser442(string tokenStream) : this(new Scanner(tokenStream)) { }
    }
}
