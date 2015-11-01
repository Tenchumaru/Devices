using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Terminal : Symbol
    {
        public static readonly Terminal AugmentedEnd = new Terminal("(end)");
        public static readonly Terminal Epsilon = new Terminal("(epsilon)");

        public Terminal(string name) : base(name) { }
    }
}
