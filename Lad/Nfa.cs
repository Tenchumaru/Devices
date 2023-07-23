using System.Text;

namespace Lad {
	/// <summary>
	/// This class represents a non-deterministic finite automata (NFA).  It contains the initial and final states of a weakly
	/// connected, directed graph of NFA states.
	/// </summary>
	public class Nfa {
		private NfaState initialState;
		private NfaState finalState;

		private Nfa(NfaState? initialState = null, NfaState? finalState = null) {
			this.initialState = initialState ?? new();
			this.finalState = finalState ?? new();
		}

		public Nfa(Symbol symbol) : this() => initialState.AddTarget(symbol, finalState);

		public static Nfa operator +(Nfa left, Nfa right) {
			Nfa rv = new(left.initialState, right.finalState);
			left.finalState.AddTarget(right.initialState);
			return rv;
		}

		public static Nfa operator /(Nfa left, Nfa right) {
			Nfa rv = new(left.initialState);
			right.finalState.AddTarget(rv.finalState);
			Nfa slash = new(new EpsilonSymbol { IsSavePoint = true });
			left.finalState.AddTarget(slash.initialState);
			slash.finalState.AddTarget(right.initialState);
			return rv;
		}

		public static Nfa operator |(Nfa left, Nfa right) {
			return Or(left, right);
		}

		public static Nfa Or(params Nfa[] nfas) {
			Nfa rv = new();
			foreach (var nfa in nfas) {
				rv.initialState.AddTarget(nfa.initialState);
				nfa.finalState.AddTarget(rv.finalState);
			}
			return rv;
		}

		public Nfa Clone() {
			Nfa rv = new(initialState, finalState);
			rv.initialState = initialState.Clone(ref rv.finalState);
			return rv;
		}

		public Nfa Count(int n) {
			if (n < 1) {
				return new Nfa(new EpsilonSymbol());
			} else if (n == 1) {
				return new Nfa(initialState, finalState);
			}
			Nfa rv = new(initialState, finalState);
			for (int i = 1; i < n; ++i) {
				initialState = initialState.Clone(ref finalState);
				rv.finalState.AddTarget(initialState);
				rv.finalState = finalState;
			}
			return rv;
		}

		public Nfa Count(int min, int max) {
			Nfa rv = Count(min);
			initialState = initialState.Clone(ref finalState);
			initialState.AddTarget(finalState);
			for (; min < max; ++min) {
				initialState = initialState.Clone(ref finalState);
				rv.finalState.AddTarget(initialState);
				rv.finalState = finalState;
			}
			return rv;
		}

		public Nfa Kleene() {
			finalState.AddTarget(initialState);
			initialState.AddTarget(finalState);
			return this;
		}

		public Nfa Plus() {
			finalState.AddTarget(initialState);
			return this;
		}

		public Nfa Question() {
			initialState.AddTarget(finalState);
			return this;
		}

#if DEBUG
		public string Dump() {
			StringBuilder sb = new();
			initialState.Dump(sb, new HashSet<NfaState>());
			return sb.ToString();
		}
#endif

		public (DfaState startState, Dictionary<string, int> acceptanceValues) MakeDfa() {
			// Create the DFA starting from this NFA's initial state.
			DfaState startState = initialState.MakeDfa();

			// Mark DFA states with transitions on accepting symbols as accepting states, remove those transitions, and collect a mapping
			// of DFA state names to state machine switch case values.
			Dictionary<string, int> acceptanceValues = new();
			startState.MarkAcceptingStates(acceptanceValues);
			return (startState, acceptanceValues);
		}

		public void SetSavePointValue(int acceptanceValue) {
			// Set the value of all save points.
			initialState.SetSavePointValue(acceptanceValue, new HashSet<NfaState>());
		}
	}
}
