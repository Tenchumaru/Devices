#pragma warning disable CA1826 // Do not use Enumerable methods on indexable collections

namespace Pard {
	class Grammar {
		public IReadOnlyList<ActionEntry> Actions => actions;
		public IReadOnlyList<GotoEntry> Gotos => gotos;
		public IReadOnlyList<Item.Set> States => states;
		private readonly IReadOnlyList<ActionEntry> actions;
		private readonly IReadOnlyList<GotoEntry> gotos;
		private readonly IReadOnlyList<Item.Set> states;

		public Grammar(IReadOnlyList<Production> productions, Nonterminal? startingSymbol) {
			// Create a collection of referenced productions starting with those productions whose left-hand side is the given starting
			// symbol or the first production's left-hand side if the starting symbol is not given.
			HashSet<Production> referencedProductions = new();
			CollectReferencedProductions(startingSymbol ?? productions[0].Lhs, productions, referencedProductions);

			// Check for unreferenced productions.
			List<Production> unreferencedProductions = productions.Except(referencedProductions).ToList();
			if (unreferencedProductions.Any()) {
				// Issue a warning if there are unreferenced productions.
				Console.Error.WriteLine("warning: {0} unreferenced productions:", unreferencedProductions.Count);
				foreach (Production unreferencedProduction in unreferencedProductions) {
					Console.Error.WriteLine(unreferencedProduction);
				}
			}

			// Create the augmented grammar (p. 222) using only referenced productions.
			Augmented augmented = new(productions[0], referencedProductions.OrderBy(p => p.Index));

			// Algorithm 4.9, p. 231
			Dictionary<Item.Set, int> items = augmented.Items().Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);
			states = items.OrderBy(p => p.Value).Select(p => p.Key).ToList();

			// Algorithm 4.10, p. 234
			// Create the action table.
			var a = from p in items
							let x = from g in p.Key.Gotos
											let t = g.Key as Terminal
											where t != null
											select new ActionEntry(stateIndex: p.Value, terminal: t, action: Action.Shift, value: items[g.Value])
							let y = from i in p.Key.AsEnumerable()
											where i.DotPosition == augmented.Productions[i.ProductionIndex].Rhs.Count
											let t = i.Lookahead
											let s = i.ProductionIndex < 0
											where !s || t == Terminal.AugmentedEnd
											select new ActionEntry(stateIndex: p.Value, terminal: t, action: s ? Action.Accept : Action.Reduce, value: i.ProductionIndex)
							from z in x.Concat(y)
							select z;

			// Account for conflicts.
			int shiftReduceConflictCount = 0, reduceReduceConflictCount = 0;
			var c = a.GroupBy(p => new KeyValuePair<int, Terminal>(p.StateIndex, p.Terminal));
			a = c.Select(x => ResolveConflict(x.ToList(), augmented.Productions, ref shiftReduceConflictCount, ref reduceReduceConflictCount));
			actions = a.ToList();
			if (shiftReduceConflictCount > 0 || reduceReduceConflictCount > 0) {
				Console.Error.Write("warning:");
				if (shiftReduceConflictCount > 0) {
					Console.Error.Write(" {0} shift-reduce conflict{1}", shiftReduceConflictCount, shiftReduceConflictCount == 1 ? "" : "s");
					if (reduceReduceConflictCount > 0) {
						Console.Error.Write(" and");
					}
				}
				if (reduceReduceConflictCount > 0) {
					Console.Error.Write(" {0} reduce-reduce conflict{1}", reduceReduceConflictCount, reduceReduceConflictCount == 1 ? "" : "s");
				}
				Console.Error.WriteLine();
			}

			// Create the goto table.
			var b = from p in items
							from g in p.Key.Gotos
							let n = g.Key as Nonterminal
							where n != null
							select new GotoEntry(stateIndex: p.Value, nonterminal: n, targetStateIndex: items[g.Value]);
			gotos = b.ToList();
		}

		private static void CollectReferencedProductions(Nonterminal lhs, IEnumerable<Production> productions, HashSet<Production> referencedProductions) {
			foreach (var production in productions.Where(p => p.Lhs == lhs)) {
				if (referencedProductions.Add(production)) {
					foreach (var item in production.Rhs.OfType<Nonterminal>()) {
						CollectReferencedProductions(item, productions, referencedProductions);
					}
				}
			}
		}

		private static ActionEntry ResolveConflict(List<ActionEntry> list, IDictionary<int, Production> productions, ref int shiftReduceConflictCount, ref int reduceReduceConflictCount) {
			switch (list.Count) {
				case 1:
					return list[0];
				case 2:
					ActionEntry left = list[0], right = list[1];
					ActionEntry? result;
					if (left.Action == Action.Shift && right.Action == Action.Reduce) {
						result = ResolveShiftReduceConflict(left, right, productions);
						if (result != null) {
							return result;
						}
						++shiftReduceConflictCount;
						return left;
					} else if (left.Action == Action.Reduce && right.Action == Action.Shift) {
						result = ResolveShiftReduceConflict(right, left, productions);
						if (result != null) {
							return result;
						}
						++shiftReduceConflictCount;
						return right;
					} else if (left.Action == Action.Reduce && right.Action == Action.Reduce) {
						// Take the reduction with the lowest production index.
						++reduceReduceConflictCount;
						return list.OrderBy(e => e.Value).First();
					}
					throw new InvalidOperationException($"unexpected actions {left.Action} and {right.Action}");
				default:
					// Take the reduction with the lowest production index and resolve the shift-reduce conflict.
					ActionEntry shift = list.Single(e => e.Action == Action.Shift);
					ActionEntry reduce = list.Where(e => e.Action == Action.Reduce).OrderBy(e => e.Value).First();
					result = ResolveShiftReduceConflict(shift, reduce, productions);
					if (result != null) {
						return result;
					}
					reduceReduceConflictCount += list.Count - 2;
					++shiftReduceConflictCount;
					return shift;
			}
		}

		private static ActionEntry? ResolveShiftReduceConflict(ActionEntry shift, ActionEntry reduce, IDictionary<int, Production> productions) {
			if (shift.Terminal.Precedence > productions[reduce.Value].Precedence) {
				return shift;
			} else if (shift.Terminal.Precedence < productions[reduce.Value].Precedence) {
				return reduce;
			} else if (shift.Terminal.Associativity == productions[reduce.Value].Associativity) {
				switch (shift.Terminal.Associativity) {
					case Associativity.None:
						Console.Error.WriteLine("warning: associativity for {0} unspecified; assuming right", shift.Terminal);
						break;
					case Associativity.Left:
						return reduce;
					case Associativity.Right:
						return shift;
					case Associativity.Nonassociative:
						Console.Error.WriteLine("warning: {0} is non-associative", shift.Terminal);
						break;
				}
			}
			return null;
		}

		class Augmented {
			internal IDictionary<int, Production> Productions => productions;
			private readonly IDictionary<int, Production> productions;
			private readonly IDictionary<Nonterminal, List<Production>> productionsByNonterminal;
			private readonly IDictionary<Symbol, HashSet<Terminal>> firstSets;

			internal Augmented(Production startProduction, IEnumerable<Production> referencedProductions) {
				List<Production> productions = new() { new Production(Nonterminal.AugmentedStart, new[] { startProduction.Lhs }, -1) };
				productions.AddRange(referencedProductions);
				this.productions = productions.ToDictionary(p => p.Index);
				productionsByNonterminal = productions.GroupBy(p => p.Lhs).ToDictionary(g => g.Key, g => g.ToList());
				firstSets = CollectFirstSets(productions);
				firstSets.Add(Terminal.AugmentedEnd, new HashSet<Terminal> { Terminal.AugmentedEnd });
				foreach (Terminal terminal in productions.SelectMany(p => p.Rhs).OfType<Terminal>().Distinct()) {
					firstSets.Add(terminal, new HashSet<Terminal> { terminal });
				}
			}

			// closure(I), p. 232
			private Item.Set Closure(IEnumerable<Item> items, Dictionary<(int i, int d, Terminal t), Terminal[]> firsts) {
				HashSet<Item> set = new(items);
				int count;
				do {
					count = set.Count;

					// for each item [A → α∙Bβ, a] in I,
					// each production B → γ in G',
					// and each terminal b in FIRST(βa)
					// such that [B → ∙γ, b] is not in I do
					var q = from i in set
									let ip = productions[i.ProductionIndex]
									where i.DotPosition < ip.Rhs.Count
									let n = ip.Rhs[i.DotPosition] as Nonterminal
									where n != null
									let f = firsts[(i.ProductionIndex, i.DotPosition + 1, i.Lookahead)]
									from p in productionsByNonterminal[n]
									from b in f
									select new Item(p.Index, 0, b);

					// add [B → ∙γ, b] to I;
					set.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.

					// until no more items can be added to I;
				} while (count < set.Count);

				// return I
				return new Item.Set(set);
			}

			// goto(I, X), p. 232
			private Item.Set Goto(Item.Set items, Dictionary<(int i, int d, Terminal t), Terminal[]> firsts, Symbol symbol) {
				// let J be the set of items [A → αX∙β, a] such that
				// [A → α∙Xβ, a] is in I;
				var q = from i in items.AsEnumerable()
								let p = productions[i.ProductionIndex]
								let d = i.DotPosition
								where d < p.Rhs.Count && p.Rhs[d] == symbol
								select new Item(i.ProductionIndex, d + 1, i.Lookahead);

				// return closure(J)
				return Closure(q, firsts);
			}

			// items(G'), p. 232
			internal IReadOnlyList<Item.Set> Items() {
				// Create a collection of symbols used in the grammar.
				HashSet<Symbol> symbols = new(productions.SelectMany(p => p.Value.Rhs));

				// Precompute FIRST of all dot positions and look-aheads for all productions.
				var q = from i in productions.Keys
								let p = productions[i]
								from d in Enumerable.Range(1, p.Rhs.Count)
								from t in symbols.OfType<Terminal>().Append(Terminal.AugmentedEnd)
								select ((i, d, t), First(p.Rhs.Skip(d).Append(t)).ToArray());
				Dictionary<(int i, int d, Terminal t), Terminal[]> firsts = q.ToDictionary((t) => t.Item1, (t) => t.Item2);

				// Create a list to hold the items added to the closure.  Their indicies will be the state indicies.
				List<Item.Set> items = new() { Closure(new[] { new Item(-1, 0, Terminal.AugmentedEnd) }, firsts) };

				// C := {closure({[S' → ∙S, $]})};
				HashSet<Item.Set> c = new(new[] { items[0] });

				// repeat
				// for each set of items I in C and each grammar symbol X
				for (int i = 0; i < items.Count; ++i) {
					Item.Set itemSet = items[i];
					foreach (Symbol symbol in symbols) {
						// such that goto(I, X) is not empty and not in C do
						Item.Set g = Goto(itemSet, firsts, symbol);
						if (g.Any()) {
							if (!c.Contains(g)) {
								// add goto(I, X) to C
								c.Add(g);
								items.Add(g);
							}

							// Add it as a target state of the source state.
							if (!itemSet.Gotos.ContainsKey(symbol)) {
								itemSet.Gotos.Add(symbol, g);
							} else if (itemSet.Gotos[symbol] != g) {
								Console.Error.WriteLine("warning: goto conflict between {0} -> {1} and {0} -> {2} on {3}",
										itemSet, itemSet.Gotos[symbol], g, symbol);
							}
						}
					}
					// until no more sets of items can be added to C
				}

				return items;
			}

			// FIRST(X), p. 189
			private HashSet<Terminal> First(IEnumerable<Symbol> symbols) {
				HashSet<Terminal> first = new();

				foreach (Symbol symbol in symbols) {
					HashSet<Terminal> symbolFirst = firstSets[symbol];
					first.UnionWith(symbolFirst);
					if (!symbolFirst.Contains(Terminal.Epsilon)) {
						first.Remove(Terminal.Epsilon);
						break;
					}
				}

				return first;
			}

			private static Dictionary<Symbol, HashSet<Terminal>> CollectFirstSets(IEnumerable<Production> productions) {
				HashSet<Production> expandedProductions = new(productions);

				// Collect all productions with additional productions not in the grammar synthesized by removing initial non-terminals
				// from productions where those initial non-terminals are the left-hand side of a production that derives ε.
				int count;
				do {
					count = expandedProductions.Count;

					// Find all ε productions.  Retrieve their left-hand sides.
					HashSet<Nonterminal> epsilonLhss = new(expandedProductions.Where(p => !p.Rhs.Any()).Select(p => p.Lhs));

					// For each of those, add to the expanded productions a production synthesized from each production with that symbol as
					// its first right-hand side symbol by removing that first right-hand side symbol.
					var q = from l in epsilonLhss
									join p in expandedProductions on l equals p.Rhs.FirstOrDefault()
									select new Production(p.Lhs, p.Rhs.Skip(1), 0);
					expandedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
				} while (count < expandedProductions.Count);

				// Remove from the expanded productions any production whose initial right-hand side is the left-hand side.  This must come
				// after the above since it's possible the symbol removal above reveals an initial right-hand side that matches its
				// left-hand side.
				expandedProductions.RemoveWhere(p => p.Lhs == p.Rhs.FirstOrDefault());

				// Create a list of non-terminal-terminal pairs to construct the FIRST sets.  Start with non-terminals that derive ε.
				var pairs = expandedProductions.Where(p => !p.Rhs.Any()).Select(p => new { Nonterminal = p.Lhs, Terminal = Terminal.Epsilon }).ToList();

				// Remove them from the expanded productions.
				expandedProductions.RemoveWhere(p => !p.Rhs.Any());

				// Add non-terminal-terminal pairs for each terminal that appears as the initial right-hand side symbol.  I needn't use
				// other terminals appearing to the right of those since I already accounted for ε productions above.
				foreach (Terminal terminal in expandedProductions.Select(p => p.Rhs.First()).OfType<Terminal>().Distinct().ToList()) {
					do {
						count = expandedProductions.Count;

						// Find all productions that have that terminal as the first symbol of their right-hand sides.
						var x = expandedProductions.Where(p => p.Rhs.First() == terminal).Select(p => new { Nonterminal = p.Lhs, Terminal = terminal });

						// Add new productions by substituting that terminal for the corresponding non-terminal as the first symbol of their
						// right-hand sides.
						var q = from p in expandedProductions
										join a in x on p.Rhs.First() equals a.Nonterminal
										select new Production(p.Lhs, new[] { terminal }, 0);
						expandedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
					} while (count < expandedProductions.Count);

					// Update the list of non-terminal-terminal pairs.
					pairs.AddRange(expandedProductions.Where(p => p.Rhs.First() == terminal).Select(p => new { Nonterminal = p.Lhs, Terminal = terminal }).Distinct());

					// Remove all productions that have that terminal as the first symbol of their right-hand sides.
					expandedProductions.RemoveWhere(p => p.Rhs.First() == terminal);
				}

				// Create a dictionary of FIRST sets for non-terminals.  The caller will add those for terminals.
				return pairs.GroupBy(a => (Symbol)a.Nonterminal, a => a.Terminal).ToDictionary(g => g.Key, g => new HashSet<Terminal>(g));
			}
		}

		public class ActionEntry {
			public int StateIndex { get; set; }
			public Terminal Terminal { get; set; }
			public Action Action { get; set; }
			public int Value { get; set; }

			public ActionEntry(int stateIndex, Terminal terminal, Action action, int value) {
				StateIndex = stateIndex;
				Terminal = terminal;
				Action = action;
				Value = value;
			}

			public override string ToString() {
				return string.Format(Action == Action.Accept ? "{0}" : "{0}{1}", Action.ToString(), Value);
			}
		}

		public class GotoEntry {
			public int StateIndex { get; set; }
			public Nonterminal Nonterminal { get; set; }
			public int TargetStateIndex { get; set; }

			public GotoEntry(int stateIndex, Nonterminal nonterminal, int targetStateIndex) {
				StateIndex = stateIndex;
				Nonterminal = nonterminal;
				TargetStateIndex = targetStateIndex;
			}

			public override string ToString() {
				return string.Format("Goto{0}", TargetStateIndex);
			}
		}

		public enum Action { None, Shift, Reduce, Accept };

		public enum Associativity { None, Left, Right, Nonassociative }
	}
}
