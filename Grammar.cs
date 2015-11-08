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
        private readonly IReadOnlyList<ActionEntry> actions;
        private readonly IReadOnlyList<GotoEntry> gotos;

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
                // Issue a warning if any.
                Console.Error.WriteLine("warning: {0} unreferenced productions:", unreferencedProductions.Count);
                foreach(var unreferencedProduction in unreferencedProductions)
                {
                    Console.Error.WriteLine(unreferencedProduction);
                }
            }

            // Create the augmented grammar (p. 222) using only referenced productions.
            var augmented = new Augmented(productions[0], referencedProductions);

            // Algorithm 4.9, p. 231
            var items = augmented.Items().Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);

            // Algorithm 4.10, p. 234
            // Create the action table.
            var a = from p in items
                    let x = from g in p.Key.Gotos
                            let t = g.Key as Terminal
                            where t != null
                            select new ActionEntry { StateIndex = p.Value, Terminal = t, Action = Action.Shift, Value = items[g.Value] }
                    let y = from i in p.Key.AsQueryable()
                            where i.DotPosition == augmented.Productions[i.ProductionIndex].Rhs.Count
                            let t = i.Lookahead
                            let s = i.ProductionIndex == 0
                            where !s || t == Terminal.AugmentedEnd
                            select new ActionEntry { StateIndex = p.Value, Terminal = t, Action = s ? Action.Accept : Action.Reduce, Value = i.ProductionIndex }
                    select x.Concat(y);

            // Account for conflicts.
            int shiftReduceConflictCount = 0, reduceReduceConflictCount = 0;
            var c = from x in a
                    from y in x
                    group y by new KeyValuePair<int, Terminal>(y.StateIndex, y.Terminal);
            var d = c.Select(x => ResolveConflict(x.Key.Key, x.Key.Value, x.ToList(), augmented.Productions, ref shiftReduceConflictCount, ref reduceReduceConflictCount));
            actions = d.ToList();
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
            // TODO:  this does not account for conflicts.
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
                }
                break;
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
            private readonly IEnumerable<Production> expandedProductions;
            private readonly IDictionary<Symbol, HashSet<Terminal>> firstSets = new Dictionary<Symbol, HashSet<Terminal>>();

            internal Augmented(Production startProduction, IEnumerable<Production> referencedProductions)
            {
                var productions = new List<Production> { new Production(Nonterminal.AugmentedStart, new[] { startProduction.Lhs }, -1) };
                productions.AddRange(referencedProductions);
                this.productions = productions;
                expandedProductions = CollectExpandedProductions(productions);
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
                    var q = from i in items.AsQueryable()
                            let ip = productions[i.ProductionIndex]
                            where i.DotPosition < ip.Rhs.Count
                            let n = ip.Rhs[i.DotPosition] as Nonterminal
                            where n != null
                            let r = ip.Rhs.Skip(i.DotPosition + 1)
                            let l = i.Lookahead
                            from p in productions.Select((x, y) => new { Production = x, Index = y })
                            where p.Production.Lhs == n
                            from b in First(r.Concat(new[] { l }))
                            select new Item(p.Index, 0, b);

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
                var q = from i in items.AsQueryable()
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

                // Create a collection of terminals.
                var terminals = new HashSet<Terminal>(productions.SelectMany(p => p.Rhs.OfType<Terminal>()));

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

            private HashSet<Terminal> First(IEnumerable<Symbol> symbols)
            {
                var first = new HashSet<Terminal>();

                foreach(var symbol in symbols)
                {
                    var symbolFirst = First(symbol);
                    first.UnionWith(symbolFirst);
                    if(!symbolFirst.Contains(Terminal.Epsilon))
                    {
                        first.Remove(Terminal.Epsilon);
                        break;
                    }
                }

                return first;
            }

            // FIRST(X), p. 189
            private HashSet<Terminal> First(Symbol symbol)
            {
                HashSet<Terminal> first;
                if(!firstSets.TryGetValue(symbol, out first))
                {
                    first = new HashSet<Terminal>();
                    var terminal = symbol as Terminal;
                    if(terminal != null)
                        first.Add(terminal);
                    else
                    {
                        var q = from p in expandedProductions
                                where p.Lhs == symbol
                                select p.Rhs.FirstOrDefault() ?? Terminal.Epsilon;
                        first.UnionWith(q.OfType<Terminal>());
                    }
                    firstSets.Add(symbol, first);
                }
                return first;
            }

            // This assists the FIRST(X) procedure by collecting all productions
            // with additional productions not in the grammar synthesized by
            // removing initial non-terminals from productions where those initial
            // non-terminals are the left-hand side of a production that derives ε.
            private static HashSet<Production> CollectExpandedProductions(IEnumerable<Production> productions)
            {
                var expandedProductions = new HashSet<Production>(productions);

                int count;
                do
                {
                    count = expandedProductions.Count;

                    // Find all ε productions.  Retrieve their left-hand sides.
                    var epsilonLhss = new HashSet<Nonterminal>(expandedProductions.Where(p => !p.Rhs.Any()).Select(p => p.Lhs));

                    // For each of those, add to the expanded productions a
                    // production synthesized from each production with that symbol
                    // as its first right-hand side symbol by excluding that symbol
                    // as its first right-hand side symbol.
                    foreach(var epsilonLhs in epsilonLhss)
                    {
                        var q = from p in expandedProductions
                                where p.Rhs.FirstOrDefault() == epsilonLhs
                                select new Production(p.Lhs, p.Rhs.Skip(1), 0);
                        expandedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
                    }

                } while(count < expandedProductions.Count);

                return expandedProductions;
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
