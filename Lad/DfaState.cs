using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    /// <summary>
    /// A collection of transitions on unique, concrete symbols to other DFA states.
    /// </summary>
    public class DfaState
    {
        public readonly List<int> AcceptingRuleIndices;
        public readonly string Name;
        public readonly Dictionary<ConcreteSymbol, DfaState> Transitions = new Dictionary<ConcreteSymbol, DfaState>();

        public DfaState(string name, IEnumerable<int> acceptingRuleIndices)
        {
            Name = name;
            AcceptingRuleIndices = new List<int>(acceptingRuleIndices);
        }

#if DEBUG
        public override string ToString()
        {
            return Name;
        }
#endif
    }
}
