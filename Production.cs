using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Production
    {
        public Nonterminal Lhs { get; private set; }
        public IReadOnlyList<Symbol> Rhs { get; private set; }
        public string Name { get; private set; }

        public Production(Nonterminal lhs, IEnumerable<Symbol> rhs)
        {
            Lhs = lhs;
            Rhs = new List<Symbol>(rhs);
            Name = String.Format("{0} -> {1}", lhs, String.Join(" ", rhs));
        }

        public override bool Equals(object obj)
        {
            var that = obj as Production;
            return !Object.ReferenceEquals(that, null) && ToString() == that.ToString();
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool operator ==(Production left, Production right)
        {
            return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(right, null) : left.Equals(right);
        }

        public static bool operator !=(Production left, Production right)
        {
            return !(left == right);
        }
    }
}
