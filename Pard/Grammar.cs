using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Grammar
    {
        public IReadOnlyList<ActionEntry> Actions { get { return actions; } }
        public IReadOnlyList<GotoEntry> Gotos { get { return gotos; } }
        public IReadOnlyList<Item.Set> States { get { return states; } }
        private readonly IReadOnlyList<ActionEntry> actions;
        private readonly IReadOnlyList<GotoEntry> gotos;
        private readonly IReadOnlyList<Item.Set> states;

        public Grammar(IReadOnlyList<Production> productions)
        {
            // Check for unreferenced productions.  Assume any production whose
            // left-hand side is the same as the left-hand side of the first
            // production is a starting production.  This isn't part of the
            // algorithm but I think it's appropriate.
            var referencedProductions = new HashSet<Production>(productions.Where(p => p.Lhs == productions[0].Lhs));
            int count;
            do
            {
                count = referencedProductions.Count;
                var q = from n in referencedProductions.SelectMany(p => p.Rhs).OfType<Nonterminal>()
                        join p in productions on n equals p.Lhs
                        select p;
                referencedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
            } while(count < referencedProductions.Count && referencedProductions.Count < productions.Count);
            var unreferencedProductions = productions.Except(referencedProductions).ToList();
            if(unreferencedProductions.Any())
            {
                // Issue a warning, if any.
                Console.Error.WriteLine("warning: {0} unreferenced productions:", unreferencedProductions.Count);
                foreach(var unreferencedProduction in unreferencedProductions)
                {
                    Console.Error.WriteLine(unreferencedProduction);
                }
            }

            // Create the augmented grammar (p. 222) using only referenced productions.
            var augmented = new Augmented(productions[0], referencedProductions.OrderBy(p => p.RuleIndex));

            // Algorithm 4.9, p. 231
            var items = augmented.Items().Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);
            states = items.OrderBy(p => p.Value).Select(p => p.Key).ToList();

            // Algorithm 4.10, p. 234
            // Create the action table.
            var a = from p in items
                    let x = from g in p.Key.Gotos
                            let t = g.Key as Terminal
                            where t != null
                            select new ActionEntry { StateIndex = p.Value, Terminal = t, Action = Action.Shift, Value = items[g.Value] }
                    let y = from i in p.Key.AsEnumerable()
                            where i.DotPosition == augmented.Productions[i.ProductionIndex].Rhs.Count
                            let t = i.Lookahead
                            let s = i.ProductionIndex == 0
                            where !s || t == Terminal.AugmentedEnd
                            select new ActionEntry { StateIndex = p.Value, Terminal = t, Action = s ? Action.Accept : Action.Reduce, Value = i.ProductionIndex }
                    from z in x.Concat(y)
                    select z;

            // Account for conflicts.
            int shiftReduceConflictCount = 0, reduceReduceConflictCount = 0;
            var c = a.GroupBy(p => new KeyValuePair<int, Terminal>(p.StateIndex, p.Terminal));
            a = c.Select(x => ResolveConflict(x.Key.Key, x.Key.Value, x.ToList(), augmented.Productions, ref shiftReduceConflictCount, ref reduceReduceConflictCount));
            actions = a.ToList();
            if(shiftReduceConflictCount > 0 || reduceReduceConflictCount > 0)
            {
                Console.Error.Write("warning:");
                if(shiftReduceConflictCount > 0)
                {
                    Console.Error.Write(" {0} shift-reduce conflict{1}", shiftReduceConflictCount, shiftReduceConflictCount == 1 ? "" : "s");
                    if(reduceReduceConflictCount > 0)
                        Console.Error.Write(" and");
                }
                if(reduceReduceConflictCount > 0)
                    Console.Error.Write(" {0} reduce-reduce conflict{1}", reduceReduceConflictCount, reduceReduceConflictCount == 1 ? "" : "s");
                Console.Error.WriteLine();
            }

            // Create the goto table.
            var b = from p in items
                    from g in p.Key.Gotos
                    let n = g.Key as Nonterminal
                    where n != null
                    select new GotoEntry { StateIndex = p.Value, Nonterminal = n, TargetStateIndex = items[g.Value] };
            gotos = b.ToList();
        }

        private ActionEntry ResolveConflict(int stateIndex, Terminal terminal, List<ActionEntry> list, IReadOnlyList<Production> productions, ref int shiftReduceConflictCount, ref int reduceReduceConflictCount)
        {
            switch(list.Count)
            {
            case 1:
                return list[0];
            case 2:
                ActionEntry left = list[0], right = list[1], result;
                switch(left.Action.ToString() + right.Action.ToString())
                {
                case "ShiftReduce":
                    result = ResolveShiftReduceConflict(left, right, productions);
                    if(result != null)
                        return result;
                    ++shiftReduceConflictCount;
                    return left;
                case "ReduceShift":
                    result = ResolveShiftReduceConflict(right, left, productions);
                    if(result != null)
                        return result;
                    ++shiftReduceConflictCount;
                    return right;
                case "ReduceReduce":
                    // Take the reduction with the lowest production index.
                    ++reduceReduceConflictCount;
                    return list.OrderBy(e => e.Value).First();
                }
                break;
            default:
                // Take the reduction with the lowest production index and
                // resolve the shift-reduce conflict.
                var shift = list.Single(e => e.Action == Action.Shift);
                var reduce = list.Where(e => e.Action == Action.Reduce).OrderBy(e => e.Value).First();
                result = ResolveShiftReduceConflict(shift, reduce, productions);
                if(result != null)
                    return result;
                reduceReduceConflictCount += list.Count - 2;
                ++shiftReduceConflictCount;
                return shift;
            }
            throw new Exception();
        }

        private ActionEntry ResolveShiftReduceConflict(ActionEntry shift, ActionEntry reduce, IReadOnlyList<Production> productions)
        {
            if(shift.Terminal.Precedence > productions[reduce.Value].Precedence)
                return shift;
            else if(shift.Terminal.Precedence < productions[reduce.Value].Precedence)
                return reduce;
            else if(shift.Terminal.Associativity == productions[reduce.Value].Associativity)
            {
                switch(shift.Terminal.Associativity)
                {
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

        class Augmented
        {
            internal IReadOnlyList<Production> Productions { get { return productions; } }
            private readonly IReadOnlyList<Production> productions;
            private readonly IDictionary<Nonterminal, List<Production>> productionsByNonterminal;
            private readonly IDictionary<Symbol, HashSet<Terminal>> firstSets;

            internal Augmented(Production startProduction, IEnumerable<Production> referencedProductions)
            {
                var productions = new List<Production> { new Production(Nonterminal.AugmentedStart, new[] { startProduction.Lhs }, 0) };
                productions.AddRange(referencedProductions);
                var q = from r in productions.Select((p, i) => new { Production = p, Index = i })
                        let p = r.Production
                        select new Production(p.Lhs, p.Rhs, r.Index, p.ActionCode, p.Associativity, p.Precedence);
                productions = q.ToList();
                this.productions = productions;
                productionsByNonterminal = productions.GroupBy(p => p.Lhs).ToDictionary(g => g.Key, g => g.ToList());
                firstSets = CollectFirstSets(productions);
                firstSets.Add(Terminal.AugmentedEnd, new HashSet<Terminal> { Terminal.AugmentedEnd });
                foreach(var terminal in productions.SelectMany(p => p.Rhs).OfType<Terminal>().Distinct())
                    firstSets.Add(terminal, new HashSet<Terminal> { terminal });
            }

            // closure(I), p. 232
            private Item.Set Closure(Item.Set items)
            {
                int count;
                do
                {
                    count = items.Count;

                    // for each item [A → α∙Bβ, a] in I,
                    // each production B → γ in G',
                    // and each terminal b in FIRST(βa)
                    // such that [B → ∙γ, b] is not in I do
                    var q = from i in items.AsEnumerable()
                            let ip = productions[i.ProductionIndex]
                            where i.DotPosition < ip.Rhs.Count
                            let n = ip.Rhs[i.DotPosition] as Nonterminal
                            where n != null
                            let f = First(ip.Rhs.Skip(i.DotPosition + 1).Concat(new[] { i.Lookahead }))
                            from p in productionsByNonterminal[n]
                            from b in f
                            select new Item(p.RuleIndex, 0, b);

                    // add [B → ∙γ, b] to I;
                    items.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.

                    // until no more items can be added to I;
                } while(count < items.Count);

                // return I
                return items;
            }

            // goto(I, X), p. 232
            private Item.Set Goto(Item.Set items, Symbol symbol)
            {
                // let J be the set of items [A → αX∙β, a] such that
                // [A → α∙Xβ, a] is in I;
                var q = from i in items.AsEnumerable()
                        let p = productions[i.ProductionIndex]
                        let d = i.DotPosition
                        where d < p.Rhs.Count && p.Rhs[d] == symbol
                        select new Item(i.ProductionIndex, d + 1, i.Lookahead);

                // return closure(J)
                return Closure(new Item.Set(q));
            }

            // items(G'), p. 232
            internal IReadOnlyList<Item.Set> Items()
            {
                // Create a collection of symbols used in the grammar.
                var symbols = new HashSet<Symbol>(productions.SelectMany(p => p.Rhs));

                // Create a list to hold the items added to the closure.  Their
                // indicies will be the state indicies.
                var items = new List<Item.Set>();

                // C := {closure({[S' → ∙S, $]})};
                var c = new HashSet<Item.Set>(new[] { Closure(new Item.Set(new[] { new Item(0, 0, Terminal.AugmentedEnd) })) });
                items.Add(c.First());

                // repeat
                int count;
                do
                {
                    count = c.Count;

                    // for each set of items I in C and each grammar symbol X
                    foreach(var itemSet in c.ToList()) // Use ToList to prevent an iteration exception.
                    {
                        foreach(var symbol in symbols)
                        {
                            // such that goto(I, X) is not empty and not in C do
                            var g = Goto(itemSet, symbol);
                            if(g.Any() && !c.Contains(g))
                            {
                                // add goto(I, X) to C
                                c.Add(g);
                                items.Add(g);
                            }

                            if(g.Any())
                            {
                                // Add it as a target state of the source state.
                                if(!itemSet.Gotos.ContainsKey(symbol))
                                {
                                    itemSet.Gotos.Add(symbol, g);
                                }
                                else if(itemSet.Gotos[symbol] != g)
                                {
                                    Console.Error.WriteLine("warning: goto conflict between {0} -> {1} and {0} -> {2} on {3}",
                                        itemSet, itemSet.Gotos[symbol], g, symbol);
                                }
                            }
                        }
                    }
                    // until no more sets of items can be added to C
                } while(count < c.Count);

                return items;
            }

            // FIRST(X), p. 189
            private HashSet<Terminal> First(IEnumerable<Symbol> symbols)
            {
                var first = new HashSet<Terminal>();

                foreach(var symbol in symbols)
                {
                    var symbolFirst = firstSets[symbol];
                    first.UnionWith(symbolFirst);
                    if(!symbolFirst.Contains(Terminal.Epsilon))
                    {
                        first.Remove(Terminal.Epsilon);
                        break;
                    }
                }

                return first;
            }

            private static Dictionary<Symbol, HashSet<Terminal>> CollectFirstSets(IEnumerable<Production> productions)
            {
                var expandedProductions = new HashSet<Production>(productions);

                // Collect all productions with additional productions not in
                // the grammar synthesized by removing initial non-terminals
                // from productions where those initial non-terminals are the
                // left-hand side of a production that derives ε.
                int count;
                do
                {
                    count = expandedProductions.Count;

                    // Find all ε productions.  Retrieve their left-hand sides.
                    var epsilonLhss = new HashSet<Nonterminal>(expandedProductions.Where(p => !p.Rhs.Any()).Select(p => p.Lhs));

                    // For each of those, add to the expanded productions a
                    // production synthesized from each production with that
                    // symbol as its first right-hand side symbol by excluding
                    // that symbol as its first right-hand side symbol.
                    foreach(var epsilonLhs in epsilonLhss)
                    {
                        var q = from p in expandedProductions
                                where p.Rhs.FirstOrDefault() == epsilonLhs
                                select new Production(p.Lhs, p.Rhs.Skip(1), 0);
                        expandedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
                    }

                } while(count < expandedProductions.Count);

                // Remove from the expanded productions any production whose
                // initial right-hand side is the left-hand side.
                expandedProductions.RemoveWhere(p => p.Lhs == p.Rhs.FirstOrDefault());

                // Create a list of non-terminal-terminal pairs to construct
                // the FIRST sets.  Start with non-terminals that derive ε.
                var pairs = expandedProductions.Where(p => !p.Rhs.Any()).Select(p => new { Nonterminal = p.Lhs, Terminal = Terminal.Epsilon }).ToList();

                // Remove them from the expanded productions.
                expandedProductions.RemoveWhere(p => !p.Rhs.Any());

                // Find the terminal of the first production that has a
                // terminal as the first symbol of its right-hand side.
                for(Terminal terminal; (terminal = expandedProductions.Select(p => p.Rhs.First()).OfType<Terminal>().FirstOrDefault()) != null; )
                {
                    // Find all productions that have that terminal as the
                    // first symbol of their right-hand sides.
                    var list = expandedProductions.Where(p => p.Rhs.First() == terminal).Select(p => new { Nonterminal = p.Lhs, Terminal = terminal }).ToList();

                    // Add them to the list of pairs.
                    pairs.AddRange(list);

                    // Remove them from the expanded productions.
                    expandedProductions.RemoveWhere(p => p.Rhs.First() == terminal);

                    // Add new productions by substituting that terminal for
                    // the corresponding non-terminal as the first symbol of
                    // their right-hand sides.
                    var q = from p in expandedProductions
                            join a in list on p.Rhs.First() equals a.Nonterminal
                            select new Production(p.Lhs, new[] { terminal }, 0);
                    expandedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
                }

                // Create a dictionary of FIRST sets for non-terminals.  The
                // caller will add those for terminals.
                return pairs.GroupBy(a => (Symbol)a.Nonterminal, a => a.Terminal).ToDictionary(g => g.Key, g => new HashSet<Terminal>(g));
            }
        }

        public class ActionEntry
        {
            public int StateIndex { get; set; }
            public Terminal Terminal { get; set; }
            public Action Action { get; set; }
            public int Value { get; set; }

            public override string ToString()
            {
                return String.Format(Action == Action.Accept ? "{0}" : "{0}{1}", Action.ToString(), Value);
            }
        }

        public class GotoEntry
        {
            public int StateIndex { get; set; }
            public Nonterminal Nonterminal { get; set; }
            public int TargetStateIndex { get; set; }

            public override string ToString()
            {
                return String.Format("Goto{0}", TargetStateIndex);
            }
        }

        public enum Action { None, Shift, Reduce, Accept };

        public enum Associativity { None, Left, Right, Nonassociative }
    }
}
