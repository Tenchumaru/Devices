using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pard.Test
{
    [TestClass]
    public class Test425
    {
        [TestMethod]
        public void ParseValidTokenStream1()
        {
            var parser = new Parser425("ibipi");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<id><id><id><EsubEsupE>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseValidTokenStream2()
        {
            var parser = new Parser425("ibibi");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<id><id><id><EsubE><EsubE>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseValidTokenStream3()
        {
            var parser = new Parser425("ibibipipi");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<id><id><id><id><id><EsupE><EsubEsupE><EsubE>", parser.Result.ToString());
        }

        [TestMethod]
        public void ParseValidTokenStream4()
        {
            var parser = new Parser425("ib{ibipi}pi");
            Assert.IsTrue(parser.Parse());
            Assert.AreEqual("<id><id><id><id><EsubEsupE><{E}><id><EsubEsupE>", parser.Result.ToString());
        }
    }

    public partial class Parser425
    {
        public readonly StringBuilder Result = new StringBuilder();

        public Parser425(string tokenStream) : this(new Scanner425(tokenStream)) { }
    }

    public class Scanner425 : Scanner
    {
        public Scanner425(string tokenStream)
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
                case 'b':
                    return new Token { Symbol = Parser425.sub };
                case 'p':
                    return new Token { Symbol = Parser425.sup };
                case 'i':
                    return new Token { Symbol = Parser425.id };
                }
                return new Token { Symbol = symbol };
            }
            return new Token { Symbol = -1 };
        }
    }
}
