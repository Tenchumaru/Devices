using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class Test439
    {
        [TestMethod]
        public void ParseValidTokenStream1()
        {
            var parser = new Parser439("1=3");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<L->id><L->id><R->L><S->L=R>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseValidTokenStream2()
        {
            var parser = new Parser439("*3=2");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<L->id><R->L><L->*R><L->id><R->L><S->L=R>", parser.Result.ToString());
        }
    }

    public partial class Parser439
    {
        public readonly StringBuilder Result = new StringBuilder();

        public Parser439(string tokenStream) : this(new Scanner(tokenStream, Parser439.id)) { }
    }
}
