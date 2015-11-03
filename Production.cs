using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Production : NamedObject
    {
        public readonly Nonterminal Lhs;
        public readonly IReadOnlyList<Symbol> Rhs;
        public readonly int RuleIndex;
        public readonly string ActionCode;
        public readonly Grammar.Associativity Associativity;
        public readonly int Precedence;

        public Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int ruleIndex, string actionCode, Grammar.Associativity associativity, int precedence)
            : this(lhs, rhs, ruleIndex, actionCode)
        {
            Associativity = associativity;
            Precedence = precedence;
        }

        public Production(Nonterminal lhs, IEnumerable<Symbol> rhs, int ruleIndex, string actionCode = null)
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
