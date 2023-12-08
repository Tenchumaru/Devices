using System.Diagnostics;
using System.Text;
using Adrezdi;

namespace Lad {
	/// <summary>
	/// This class represents a state of a NFA.  It contains a collection of transitions on symbols to other NFA states and a unique
	/// identifying number.
	/// </summary>
	public class NfaState {
		public int Number { get; }
		private readonly Multimap<Symbol, NfaState> transitions = new();
		private static int nextNumber;

		public NfaState() => Number = ++nextNumber;

		public void AddTarget(NfaState state) => AddTarget(new EpsilonSymbol(), state);

		public void AddTarget(Symbol symbol, NfaState state) => transitions.Add(symbol.MakeDegenerate(), state);

		public NfaState Clone(ref NfaState state) => Clone(new Dictionary<NfaState, NfaState>(), ref state);

		private NfaState Clone(Dictionary<NfaState, NfaState> clonedStates, ref NfaState state) {
			if (!clonedStates.TryGetValue(this, out NfaState? clone)) {
				// This state has not yet cloned itself.  Do so now.
				clone = new NfaState();
				clonedStates.Add(this, clone);
				foreach (KeyValuePair<Symbol, NfaState> target in transitions) {
					clone.transitions.Add(target.Key, target.Value.Clone(clonedStates, ref state));
				}
			}
			if (state == this) {
				state = clone;
			}
			return clone;
		}

		/// <summary>
		/// Implement the subset construction algorithm given in figure 3.25 on page 118 of the Dragon book.
		/// </summary>
		/// <returns>The DFA from applying the algorithm to this initial NFA state.</returns>
		public DfaState MakeDfa() {
			// If any NFA state transitions on BOL, prepend a Kleened BOL to every other concrete transition.  Do this because BOL is a
			// concrete symbol and all transitions must accept it if it is present.
			if (HasBol()) {
				AddKleenedBols();
			}

			// p. 118, Fig. 3.25
			Queue<EpsilonClosure> queue = new();
			EpsilonClosure startClosure = MakeEpsilonClosure(new[] { this });
			DfaState startState = new(startClosure.Name, 0);
			startClosure.Dfa = startState;
			Dictionary<string, DfaState> dfaStates = new() {
				{ startClosure.Name, startState },
			};
			queue.Enqueue(startClosure);
			while (queue.Any()) {
				EpsilonClosure closure = queue.Dequeue();
				var q = closure.Nfas.SelectMany(s => s.transitions).Select(p => p.Key).OfType<ConcreteSymbol>();

				// Add this extra step not in the algorithm to split overlapping symbols.  Certain mixtures of any, range, and simple
				// symbols can overlap. Although these split symbols might not appear in the grammar, the Move method uses inclusion,
				// not identity.
				HashSet<ConcreteSymbol> inputSymbols = SplitOverlappingSymbols(q);

				foreach (ConcreteSymbol inputSymbol in inputSymbols) {
					HashSet<NfaState> nfaTargets = Move(closure.Nfas, inputSymbol);
					EpsilonClosure targetClosure = MakeEpsilonClosure(nfaTargets);
					if (!dfaStates.TryGetValue(targetClosure.Name, out DfaState? dfaState)) {
						dfaState = new DfaState(targetClosure.Name, inputSymbol.SaveForAcceptance);
						dfaStates.Add(targetClosure.Name, dfaState);
						targetClosure.Dfa = dfaState;
						queue.Enqueue(targetClosure);
					}
					Debug.Assert(!closure.Dfa!.Transitions.ContainsKey(inputSymbol));
					closure.Dfa.Transitions.Add(inputSymbol, dfaState);
				}
			}
			return startState;
		}

		private bool HasBol() {
			HashSet<NfaState> nfaStates = new();
			Queue<NfaState> queue = new(new[] { this });
			while (queue.Any()) {
				NfaState nfaState = queue.Dequeue();
				if (nfaStates.Add(nfaState)) {
					if (nfaState.transitions.Any(p => p.Key is BolSymbol)) {
						return true;
					}
					queue.EnqueueRange(nfaState.transitions.Where(p => p.Key is EpsilonSymbol).Select(p => p.Value));
				}
			}
			return false;
		}

		private void AddKleenedBols() {
			KeyValuePair<Symbol, NfaState>[] bolableTransitions = transitions.Where(t => t.Key is ConcreteSymbol and not BolSymbol).ToArray();
			if (bolableTransitions.Any()) {
				Multimap<Symbol, NfaState> newTransitions = new(transitions.Where(t => t.Key is EpsilonSymbol or BolSymbol));
				foreach (KeyValuePair<Symbol, NfaState> transition in newTransitions.Where(t => t.Key is EpsilonSymbol)) {
					transition.Value.AddKleenedBols();
				}
				NfaState nextState = new();
				nextState.transitions.AddRange(bolableTransitions);
				newTransitions.Add(BolSymbol.Value, nextState);
				newTransitions.Add(new EpsilonSymbol(), nextState);
				transitions.Clear();
				transitions.AddRange(newTransitions);
			} else {
				foreach (KeyValuePair<Symbol, NfaState> transition in transitions.Where(t => t.Key is EpsilonSymbol)) {
					transition.Value.AddKleenedBols();
				}
			}
		}

		private static HashSet<ConcreteSymbol> SplitOverlappingSymbols(IEnumerable<ConcreteSymbol> inputSymbols) {
			var l = new HashSet<ConcreteSymbol>(inputSymbols).ToList();
			for (int i = 0; i < l.Count - 1; ++i) {
				for (int j = i + 1; j < l.Count; ++j) {
					ConcreteSymbol? intersection = l[i] & l[j];
					if (intersection != null) {
						ConcreteSymbol? left = l[i] - l[j];
						if (left is not null) {
							l.Add(left);
						}
						ConcreteSymbol? right = l[j] - l[i];
						if (right is not null) {
							l.Add(right);
						}
						l[i] = intersection;
						l[j] = l.Last();
						l.RemoveAt(l.Count - 1);
						--i;
						break;
					}
				}
			}
			return new HashSet<ConcreteSymbol>(l);
		}

		private static HashSet<NfaState> Move(HashSet<NfaState> value, ConcreteSymbol inputSymbol) {
#if DEBUG
			var q = from s in value
							from t in s.transitions
							where inputSymbol.IsIn(t.Key)
							select t.Value;
#else
			var q = value.SelectMany(s => s.transitions).Where(t => inputSymbol.IsIn(t.Key)).Select(t => t.Value);
#endif
			return new HashSet<NfaState>(q);
		}

		public void Dump(StringBuilder sb, HashSet<NfaState> dumpedStates) {
			if (dumpedStates.Add(this)) {
				sb.AppendLine($"{Number}:");
				foreach (KeyValuePair<Symbol, NfaState> transition in transitions) {
					sb.AppendLine($"\t{transition.Key} -> {transition.Value.Number}");
				}
				foreach (KeyValuePair<Symbol, NfaState> transition in transitions) {
					transition.Value.Dump(sb, dumpedStates);
				}
			}
		}

#if DEBUG
		public override string ToString() => Number.ToString();
#endif

		private static EpsilonClosure MakeEpsilonClosure(IEnumerable<NfaState> nfaStates) {
			// p. 119, Fig. 3.26
			HashSet<NfaState> closure = new();
			Queue<NfaState> queue = new(nfaStates);
			List<int> numbers = new();
			while (queue.Any()) {
				NfaState next = queue.Dequeue();
				if (closure.Add(next)) {
					numbers.Add(next.Number);
					queue.EnqueueRange(next.transitions.Where(p => p.Key is EpsilonSymbol).Select(p => p.Value));
				}
			}
			numbers.Sort();
			return new EpsilonClosure(string.Join(", ", numbers), closure);
		}

		public bool SetSavePoint(int acceptanceValue, HashSet<NfaState> nfaStates) {
			if (nfaStates.Add(this)) {
				foreach (KeyValuePair<Symbol, NfaState> pair in transitions) {
					if (pair.Key is ConcreteSymbol symbol && symbol.SaveForAcceptance < 0) {
						symbol.SaveForAcceptance = acceptanceValue;
						return true;
					}
					if (pair.Value.SetSavePoint(acceptanceValue, nfaStates)) {
						return true;
					}
				}
			}
			return false;
		}

		public bool CanReachOnEpsilon(NfaState finalState) {
			return CanReachOnEpsilon(finalState, new HashSet<NfaState>());
		}

		private bool CanReachOnEpsilon(NfaState finalState, HashSet<NfaState> nfaStates) {
			if (nfaStates.Add(this)) {
				return this == finalState || transitions.Any(p => p.Key is EpsilonSymbol && p.Value.CanReachOnEpsilon(finalState, nfaStates));
			}
			return false;
		}

		public void RemoveEpsilonTransitions() {
			RemoveEpsilonTransitions(new HashSet<NfaState>());
		}

		private void RemoveEpsilonTransitions(HashSet<NfaState> nfaStates) {
			if (nfaStates.Add(this)) {
				if (transitions.Any()) {
					while (transitions.All(t => t.Key is EpsilonSymbol)) {
						var newTransitions = new Multimap<Symbol, NfaState>(transitions.SelectMany(t => t.Value.transitions).Where(t => t.Key is not EpsilonSymbol || t.Value != this));
						transitions.Clear();
						transitions.AddRange(newTransitions);
					}
					foreach (KeyValuePair<Symbol, NfaState> item in transitions) {
						item.Value.RemoveEpsilonTransitions(nfaStates);
					}
				}
			}
		}

		public void CreateSavePoint() {
			// Find all concrete symbols reachable from here through epsilon symbols and set them as save points.
			CreateSavePoint(new HashSet<NfaState>());
		}

		private void CreateSavePoint(HashSet<NfaState> nfaStates) {
			if (nfaStates.Add(this)) {
				foreach (KeyValuePair<Symbol, NfaState> item in transitions.Where(t => t.Key is EpsilonSymbol).SelectMany(t => t.Value.transitions)) {
					item.Value.CreateSavePoint(nfaStates);
				}
				foreach (var symbol in transitions.Select(t => t.Key).OfType<ConcreteSymbol>()) {
					// Mark this symbol as a save point.  The SetSavePoint method will set it.
					symbol.SaveForAcceptance = -1;
				}
			}
		}

		private void CollectAcceptanceValues(HashSet<int> acceptanceValues, HashSet<NfaState> nfaStates) {
			if (nfaStates.Add(this)) {
				acceptanceValues.AddRange(transitions.Select(t => t.Key).OfType<ConcreteSymbol>().Select(s => s.SaveForAcceptance).Where(n => n > 0));
				foreach (KeyValuePair<Symbol, NfaState> item in transitions.Where(t => t.Key is EpsilonSymbol)) {
					item.Value.CollectAcceptanceValues(acceptanceValues, nfaStates);
				}
				Debug.Assert(!acceptanceValues.Contains(-1));
			}
		}

		private class EpsilonClosure {
			public readonly string Name;
			public readonly HashSet<NfaState> Nfas;
			public DfaState? Dfa;

			public EpsilonClosure(string name, HashSet<NfaState> nfas) {
				Name = name;
				Nfas = nfas;
				Dfa = null;
			}

			public override string ToString() => Name;
		}
	}
}
