using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Terminal : Symbol
    {
        public readonly Grammar.Associativity Associativity;
        public readonly int Precedence;

        public static readonly Terminal AugmentedEnd = new Terminal("(end)", null, Grammar.Associativity.None, 0);
        public static readonly Terminal Epsilon = new Terminal("(epsilon)", null, Grammar.Associativity.None, 0);

        public Terminal(string name, string typeName, Grammar.Associativity associativity, int precedence)
            : base(name, typeName)
        {
            Associativity = associativity;
            Precedence = precedence;
        }

        public static string FormatLiteralName(string s)
        {
            // TODO: perform unescaping on the value.
            if(s.Length != 1)
            {
                throw new ArgumentException(String.Format("invalid literal name: '{0}'", s));
            }
            return "'" + s + "'";
        }
    }
}
