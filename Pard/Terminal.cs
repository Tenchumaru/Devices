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
        public readonly int Value;

        private static int lastValue;

        public static readonly Terminal AugmentedEnd = new Terminal("(end)", null, Grammar.Associativity.None, 0);
        public static readonly Terminal Epsilon = new Terminal("(epsilon)", null, Grammar.Associativity.None, 0);
        public static readonly Terminal Error = new Terminal("(error)", null, Grammar.Associativity.None, 0);

        public Terminal(string name, string typeName, Grammar.Associativity associativity, int precedence)
            : this(name, typeName, associativity, precedence, --lastValue)
        {
        }

        public Terminal(string name, string typeName, Grammar.Associativity associativity, int precedence, int value)
            : base(name, typeName)
        {
            Associativity = associativity;
            Precedence = precedence;
            Value = value;
        }

        public static string FormatLiteralName(string s)
        {
            // TODO: perform unescaping on the value.
            return s.Length == 1 ? "'" + s + "'" : null;
        }
    }
}
