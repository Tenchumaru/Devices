using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class Test433
    {
        [TestMethod]
        public void ParseValidTokenStream1()
        {
            var parser = new Parser433("1+2*3");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual(7, parser.Result);
        }

        [TestMethod]
        public void ParseValidTokenStream2()
        {
            var parser = new Parser433("(1+2)*3");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual(9, parser.Result);
        }
    }

    public partial class Parser433
    {
        public int Result;

        public Parser433(string tokenStream) : this(new Scanner(tokenStream, Parser433.id)) { }
    }
}
