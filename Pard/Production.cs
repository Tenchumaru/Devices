using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    public class Production : NamedObject
    {
        internal readonly Nonterminal Lhs;
        internal readonly IReadOnlyList<Symbol> Rhs;
        public readonly int RuleIndex;
        public readonly string ActionCode;
        internal readonly Grammar.Associativity Associativity;
        public readonly int Precedence;

        internal Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int ruleIndex, string actionCode, Grammar.Associativity associativity, int precedence)
            : this(lhs, rhs, ruleIndex, actionCode)
        {
            Associativity = associativity;
            Precedence = precedence;
        }

        internal Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int ruleIndex, string actionCode = null)
            : base(String.Format("{0} -> {1}", lhs, String.Join(" ", rhs)))
        {
            Lhs = lhs;
            Rhs = new List<Symbol>(rhs);
            RuleIndex = ruleIndex;
            ActionCode = actionCode;
            var precedenceTerminal = (Terminal)Rhs.LastOrDefault(s => s is Terminal);
            if(precedenceTerminal != null)
            {
                Associativity = precedenceTerminal.Associativity;
                Precedence = precedenceTerminal.Precedence;
            }
        }
    }
}
