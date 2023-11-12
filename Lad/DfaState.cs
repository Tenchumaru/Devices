namespace Lad {
	/// <summary>
	/// This class represents a state in a deterministic finite automata (DFA).  It contains a collection of transitions on concrete
	/// symbols to other DFA states, a unique name, and an acceptance value if this is an accepting state of the DFA.
	/// </summary>
	public class DfaState {
		public string Name { get; }
		public int Acceptance { get; private set; }
		public int SaveForAcceptance { get; }
		public Dictionary<ConcreteSymbol, DfaState> Transitions { get; } = new();

		public DfaState(string name, int saveForAcceptance) {
			Name = name;
			SaveForAcceptance = saveForAcceptance;
		}

#if DEBUG
		public override string ToString() => Name;
#endif

		/// <summary>
		/// Remove all accepting transitions.  If there were any such transitions, use the smallest value of those transitions as the
		/// acceptance value of this DFA state.  Add an entry to the case value mapping given in <paramref name="dfaCaseValues"/>.
		/// Propagate this operation to all reachable DFA states.
		/// </summary>
		/// <param name="dfaCaseValues">The mapping of DFA state names to values used in case statements in the switch statement that
		/// implements the state machine.</param>
		public void MarkAcceptingStates(Dictionary<string, int> dfaCaseValues) {
			if (!dfaCaseValues.ContainsKey(Name)) {
				dfaCaseValues.Add(Name, dfaCaseValues.Count + 1);
				var q = Transitions.Select(p => p.Key).OfType<AcceptingSymbol>();
				foreach (var acceptingSymbol in q) {
					if (Acceptance == 0 || acceptingSymbol.Value < Acceptance) {
						Acceptance = acceptingSymbol.Value;
					}
					Transitions.Remove(acceptingSymbol);
				}
				foreach (var transition in Transitions) {
					transition.Value.MarkAcceptingStates(dfaCaseValues);
				}
			}
		}

		public void Dump(System.Text.StringBuilder sb, bool wantsFullName = false) {
			Dictionary<DfaState, (int, bool)> dumpedStates = new();
			Func<DfaState, string> fn = wantsFullName ? (d) => d.Name : (d) => dumpedStates[d].Item1.ToString();
			Dump(sb, fn, dumpedStates);
		}

		private void Dump(System.Text.StringBuilder sb, Func<DfaState, string> fn, Dictionary<DfaState, (int, bool)> dumpedStates) {
			if (dumpedStates.TryAdd(this, (dumpedStates.Count, false)) || !dumpedStates[this].Item2) {
				dumpedStates[this] = (dumpedStates[this].Item1, true);
				sb.Append(fn(this));
				if (Acceptance != 0) {
					sb.Append($" accepting {Acceptance}");
				}
				if (SaveForAcceptance != 0) {
					sb.Append($" saving {SaveForAcceptance}");
				}
				sb.AppendLine(Transitions.Any() ? ":" : ".");
				foreach (KeyValuePair<ConcreteSymbol, DfaState> transition in Transitions) {
					dumpedStates.TryAdd(transition.Value, (dumpedStates.Count, false));
					sb.AppendLine($"\t{transition.Key} -> {fn(transition.Value)}");
				}
				foreach (KeyValuePair<ConcreteSymbol, DfaState> transition in Transitions) {
					transition.Value.Dump(sb, fn, dumpedStates);
				}
			}
		}
	}
}
