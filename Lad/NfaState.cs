using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lad
{
    /// <summary>
    /// A collection of transitions on symbols to other NFA states.
    /// </summary>
    public class NfaState
    {
        public int Number { get; private set; }
        private Multimap<Symbol, NfaState> transitions = new Multimap<Symbol, NfaState>();
        private int? acceptingRuleIndex;
        private static int nextNumber;

        public NfaState()
        {
            Number = ++nextNumber;
        }

        public NfaState(int acceptingRuleIndex)
            : this()
        {
            this.acceptingRuleIndex = acceptingRuleIndex;
        }

        public void AddTarget(NfaState state)
        {
            AddTarget(EpsilonSymbol.Value, state);
        }

        public void AddTarget(Symbol symbol, NfaState state)
        {
            transitions.Add(symbol, state);
        }

        public NfaState Clone(ref NfaState state)
        {
            var clonedStates = new Dictionary<NfaState, NfaState>();
            return Clone(clonedStates, ref state);
        }

        private NfaState Clone(Dictionary<NfaState, NfaState> clonedStates, ref NfaState state)
        {
            if(!clonedStates.TryGetValue(this, out NfaState clone))
            {
                // This state has not yet cloned itself.  Do so now.
                Debug.Assert(acceptingRuleIndex == null);
                clone = new NfaState();
                clonedStates.Add(this, clone);
                foreach(KeyValuePair<Symbol, NfaState> target in transitions)
                    clone.transitions.Add(target.Key, target.Value.Clone(clonedStates, ref state));
            }
            if(state == this)
                state = clone;
            return clone;
        }

        public DfaState CreateDfa()
        {
            var dfaStates = new Dictionary<string, DfaState>();
            var queue = new Queue<KeyValuePair<DfaState, HashSet<NfaState>>>();
            HashSet<NfaState> closure = CreateEpsilonClosure();
            string closureName = CreateClosureName(closure);
            var startState = new DfaState(closureName, GetAcceptingRuleIndices(closure));
            queue.Enqueue(new KeyValuePair<DfaState, HashSet<NfaState>>(startState, closure));
            while(queue.Count > 0)
            {
                var pair = queue.Dequeue();
                if(!dfaStates.TryGetValue(pair.Key.Name, out DfaState state))
                {
                    state = pair.Key;
                    dfaStates.Add(state.Name, state);
                    IDictionary<ConcreteSymbol, HashSet<NfaState>> transitions = CreateTargetEpsilonClosures(pair.Value);
                    foreach(var transition in transitions)
                    {
                        string targetName = CreateClosureName(transition.Value);
                        DfaState targetState = queue.FirstOrDefault(p => p.Key.Name == targetName).Key;
                        if(targetState != null)
                            state.Transitions.Add(transition.Key, targetState);
                        else
                        {
                            if(!dfaStates.TryGetValue(targetName, out targetState))
                                targetState = new DfaState(targetName, GetAcceptingRuleIndices(transition.Value));
                            state.Transitions.Add(transition.Key, targetState);
                            queue.Enqueue(new KeyValuePair<DfaState, HashSet<NfaState>>(targetState, transition.Value));
                        }
                    }
                }
            }
            return startState;
        }

        [Conditional("DEBUG")]
        public void Dump(HashSet<NfaState> dumpedStates)
        {
#if DEBUG
            if(dumpedStates.Contains(this))
                return;
            dumpedStates.Add(this);
            Debug.WriteLine(String.Format("{0} {1}:", acceptingRuleIndex, Number));
            foreach(KeyValuePair<Symbol, NfaState> transition in transitions)
                Debug.WriteLine(String.Format("\t{0} -> {1}", transition.Key, transition.Value.Number));
            foreach(KeyValuePair<Symbol, NfaState> transition in transitions)
                transition.Value.Dump(dumpedStates);
#endif
        }

        private static IEnumerable<int> GetAcceptingRuleIndices(HashSet<NfaState> closure)
        {
            return closure.Where(n => n.acceptingRuleIndex.HasValue).Select(n => n.acceptingRuleIndex.Value);
        }

#if DEBUG
        public override string ToString()
        {
            return Number.ToString();
        }
#endif

        private static Dictionary<ConcreteSymbol, HashSet<NfaState>> CreateTargetEpsilonClosures(HashSet<NfaState> closure)
        {
            // Create the target closures.
            var q1 = from c in closure
                     from t in c.transitions
                     let s = t.Key as ConcreteSymbol
                     where s != null
                     group new { Symbol = s, State = t.Value } by s;
            var targetClosures = q1.ToDictionary(g => g.Key, g => new HashSet<NfaState>(g.SelectMany(e => e.State.CreateEpsilonClosure())));

            // If any symbols contain other symbols, have the contained symbols
            // also transition to the same states as the containing symbols.
            var q2 = from s1 in targetClosures.Keys
                     from s2 in targetClosures.Keys
                     where s1 != s2 && s1.Contains(s2)
                     select new { ContainingSymbol = s1, ContainedSymbol = s2, TargetStates = targetClosures[s1] };
            var list = q2.ToList();
            list.ForEach(e => targetClosures[e.ContainedSymbol].UnionWith(e.TargetStates));

            // Remove the contained symbols from the containing symbols.
            var groupings = list.GroupBy(a => a.ContainingSymbol, a => a.ContainedSymbol);
            foreach(var g in groupings)
            {
                HashSet<NfaState> targets = targetClosures[g.Key];
                ConcreteSymbol symbol = g.Key.Remove(g.AsEnumerable());
                targetClosures.Remove(g.Key);
                if(symbol != null)
                    targetClosures.Add(symbol, targets);
            }

            return targetClosures;
        }

        private HashSet<NfaState> CreateEpsilonClosure()
        {
            var closure = new HashSet<NfaState>();
            var queue = new Queue<NfaState>();
            queue.Enqueue(this);
            while(queue.Count > 0)
            {
                NfaState next = queue.Dequeue();
                if(closure.Add(next))
                    next.transitions.FindAll(EpsilonSymbol.Value).ForEach(s => queue.Enqueue(s));
            }
            return closure;
        }

        private string CreateClosureName(IEnumerable<NfaState> closure)
        {
            var sb = new StringBuilder();
            foreach(NfaState state in closure)
                sb.AppendFormat("{0},", state.Number);
            return sb.ToString(0, sb.Length - 1);
        }
    }
}
