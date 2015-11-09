using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class Test422
    {
        [TestMethod]
        public void ParseValidTokenStream1()
        {
            var parser = new Parser422("1+2*3");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual(7, parser.Result);
        }

        [TestMethod]
        public void ParseValidTokenStream2()
        {
            var parser = new Parser422("(1+2)*3");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual(9, parser.Result);
        }
    }

    public partial class Parser422
    {
        public int Result;

        public Parser422(string tokenStream) : this(new Scanner(tokenStream, Parser433.id)) { }
    }
}
