using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pard
{
    class YaccInput : IGrammarInput
    {
        public IReadOnlyList<Production> Read(System.IO.TextReader reader, Options options)
        {
            throw new NotImplementedException();
        }
    }
}
