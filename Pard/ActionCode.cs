using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class ActionCode
    {
        internal readonly string Code;
        internal readonly int LineNumber;

        internal ActionCode(string code, int lineNumber)
        {
            Code = code;
            LineNumber = lineNumber;
        }
    }
}
