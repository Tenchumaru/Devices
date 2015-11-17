using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    class Token
    {
        public static readonly Token End = new Token { Symbol = -1 };
        public int Symbol;
        public object Value;
    }
}
