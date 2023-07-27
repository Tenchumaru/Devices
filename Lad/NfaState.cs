using System.Diagnostics;
using System.Text;

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
			// If any NFA state transitions on BOL, prepend a Kleened BOL to every other concrete transition.
			if (CheckForBol()) {
				AddKleenedBols();
			}

			// p. 118, Fig. 3.25
			Queue<EpsilonClosure> queue = new();
			EpsilonClosure startClosure = MakeEpsilonClosure(new[] { this });
			DfaState startState = new(startClosure.Name, startClosure.SaveForAcceptance);
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
						dfaState = new DfaState(targetClosure.Name, targetClosure.SaveForAcceptance);
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

		private bool CheckForBol() {
			HashSet<NfaState> nfaStates = new();
			Queue<NfaState> queue = new(new[] { this });
			while (queue.Any()) {
				NfaState nfaState = queue.Dequeue();
				if (!nfaStates.Contains(nfaState)) {
					nfaStates.Add(nfaState);
					if (nfaState.transitions.Any(p => p.Key is BolSymbol)) {
						return true;
					}
					foreach (NfaState targetState in nfaState.transitions.Where(p => p.Key is EpsilonSymbol).Select(p => p.Value)) {
						queue.Enqueue(targetState);
					}
				}
			}
			return false;
		}

		private void AddKleenedBols() {
			Queue<NfaState> queue = new(new[] { this });
			while (queue.Any()) {
				NfaState nfaState = queue.Dequeue();
				List<KeyValuePair<Symbol, NfaState>> preBolTransitions = new();
				List<KeyValuePair<Symbol, NfaState>> postBolTransitions = new();
				foreach (KeyValuePair<Symbol, NfaState> transition in nfaState.transitions) {
					if (transition.Key is EpsilonSymbol) {
						queue.Enqueue(transition.Value);
						preBolTransitions.Add(transition);
					} else if (transition.Key is BolSymbol) {
						preBolTransitions.Add(transition);
					} else {
						Debug.Assert(transition.Key is ConcreteSymbol);
						postBolTransitions.Add(transition);
					}
				}
				if (postBolTransitions.Any()) {
					nfaState.transitions.Clear();
					nfaState.transitions.AddRange(preBolTransitions);
					NfaState bolState = new();
					bolState.transitions.AddRange(postBolTransitions);
					nfaState.transitions.Add(new EpsilonSymbol(), bolState);
					nfaState.transitions.Add(BolSymbol.Value, bolState);
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

		[Conditional("DEBUG")]
		public void Dump(StringBuilder sb, HashSet<NfaState> dumpedStates) {
#if DEBUG
			if (!dumpedStates.Contains(this)) {
				dumpedStates.Add(this);
				sb.AppendLine($"{Number}:");
				foreach (KeyValuePair<Symbol, NfaState> transition in transitions) {
					sb.AppendLine($"\t{transition.Key} -> {transition.Value.Number}");
				}
				foreach (KeyValuePair<Symbol, NfaState> transition in transitions) {
					transition.Value.Dump(sb, dumpedStates);
				}
			}
#endif
		}

#if DEBUG
		public override string ToString() => Number.ToString();
#endif

		private static EpsilonClosure MakeEpsilonClosure(IEnumerable<NfaState> nfaStates) {
			// p. 119, Fig. 3.26
			HashSet<NfaState> closure = new();
			Queue<NfaState> queue = new(nfaStates);
			List<int> numbers = new();
			int saveForAcceptance = 0;
			while (queue.Any()) {
				NfaState next = queue.Dequeue();
				if (closure.Add(next)) {
					numbers.Add(next.Number);
					foreach (var epsilonTransition in next.transitions.Where(p => p.Key is EpsilonSymbol).Select(p => new { Key = (EpsilonSymbol)p.Key, p.Value })) {
						queue.Enqueue(epsilonTransition.Value);
						if (epsilonTransition.Key.IsSavePoint) {
							saveForAcceptance = epsilonTransition.Key.SaveForAcceptance;
						}
					}
				}
			}
			numbers.Sort();
			return new EpsilonClosure(string.Join(", ", numbers), closure, saveForAcceptance);
		}

		public bool SetSavePointValue(int acceptanceValue, HashSet<NfaState> nfaStates) {
			if (!nfaStates.Contains(this)) {
				nfaStates.Add(this);
				foreach (var pair in transitions) {
					if (pair.Key is EpsilonSymbol epsilon && epsilon.IsSavePoint) {
						epsilon.SaveForAcceptance = acceptanceValue;
						return true;
					}
					if (pair.Value.SetSavePointValue(acceptanceValue, nfaStates)) {
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
			if (!nfaStates.Contains(this)) {
				nfaStates.Add(this);
				return this == finalState || transitions.Any(p => p.Key is EpsilonSymbol && p.Value.CanReachOnEpsilon(finalState, nfaStates));
			}
			return false;
		}

		private class EpsilonClosure {
			public readonly string Name;
			public readonly HashSet<NfaState> Nfas;
			public DfaState? Dfa;
			public readonly int SaveForAcceptance;

			public EpsilonClosure(string name, HashSet<NfaState> nfas, int saveForAcceptance) {
				Name = name;
				Nfas = nfas;
				Dfa = null;
				SaveForAcceptance = saveForAcceptance;
			}
		}
	}
}
