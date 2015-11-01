using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Nonterminal : Symbol
    {
        public static readonly Nonterminal AugmentedStart = new Nonterminal("(start)");

        public Nonterminal(string name) : base(name) { }
    }
}
