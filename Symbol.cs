using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    abstract class Symbol
    {
        public readonly string Name;

        public Symbol(string name)
        {
            Name = name;
        }

        public override bool Equals(object obj)
        {
            var that = obj as Symbol;
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

        public static bool operator ==(Symbol left, Symbol right)
        {
            return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(right, null) : left.Equals(right);
        }

        public static bool operator !=(Symbol left, Symbol right)
        {
            return !(left == right);
        }
    }
}
