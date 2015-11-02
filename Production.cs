using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Production : NamedObject
    {
        public Nonterminal Lhs { get; private set; }
        public IReadOnlyList<Symbol> Rhs { get; private set; }

        public Production(Nonterminal lhs, IEnumerable<Symbol> rhs)
            : base(String.Format("{0} -> {1}", lhs, String.Join(" ", rhs)))
        {
            Lhs = lhs;
            Rhs = new List<Symbol>(rhs);
        }
    }
}
