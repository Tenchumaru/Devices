using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    public class Nfa
    {
        private NfaState initialState, finalState;
        public Nfa TrailingContext { set { throw new NotImplementedException(); } }

        public Nfa(Symbol symbol)
        {
            initialState = new NfaState();
            finalState = new NfaState();
            initialState.AddTarget(symbol, finalState);
        }

        public Nfa(List<Nfa> those)
        {
            initialState = new NfaState();
            finalState = new NfaState();
            foreach(Nfa that in those)
            {
                initialState.AddTarget(that.initialState);
                that.finalState.AddTarget(finalState);
            }
        }

        public Nfa(Nfa that)
        {
            finalState = that.finalState;
            initialState = that.initialState.Clone(ref finalState);
        }

        public void AddBol()
        {
            throw new NotImplementedException();
        }

        public void Concat(Nfa that)
        {
            finalState.AddTarget(that.initialState);
            finalState = that.finalState;
        }

        public DfaState CreateDfa()
        {
            return initialState.CreateDfa();
        }

        public void Count(int min, int max)
        {
            var one = new Nfa(this);
            for(int i = 1; i < min; ++i)
                this.Concat(new Nfa(one));
            one.initialState.AddTarget(one.finalState);
            for(int i = Math.Max(1, min); i < max; ++i)
                this.Concat(new Nfa(one));
            if(min == 0)
                initialState.AddTarget(finalState);
        }

        public void Kleene()
        {
            var s = new NfaState();
            s.AddTarget(initialState);
            var t = new NfaState();
            finalState.AddTarget(t);
            t.AddTarget(initialState);
            s.AddTarget(t);
            initialState = s;
            finalState = t;
        }

        public void Or(Nfa that)
        {
            var s = new NfaState();
            s.AddTarget(initialState);
            s.AddTarget(that.initialState);
            var t = new NfaState();
            finalState.AddTarget(t);
            that.finalState.AddTarget(t);
            initialState = s;
            finalState = t;
        }

        public void Plus()
        {
            var nfa = new Nfa(this);
            nfa.Kleene();
            this.Concat(nfa);
        }

        public void SetRuleGroupNames(IEnumerable<string> ruleGroupNames)
        {
            throw new NotImplementedException();
        }

        public void Finish(int acceptingRuleIndex)
        {
            var s = new NfaState(acceptingRuleIndex);
            finalState.AddTarget(s);
            finalState = s;
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public void Dump()
        {
            initialState.Dump(new HashSet<NfaState>());
        }
    }
}
