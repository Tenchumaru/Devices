using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    class Grammar
    {
        public List<Production> Productions { get; set; }

        // Algorithm 4.10, p. 234
        public object ConstructTable()
        {
            // Check for unreferenced productions.  Assume any production whose
            // left-hand side is the same as the left-hand side of the first
            // production is a starting production.  This isn't part of the
            // algorithm but I think it's appropriate.
            var referencedProductions = new HashSet<Production>(Productions.Where(p => p.Lhs == Productions[0].Lhs));
            int count;
            do
            {
                count = referencedProductions.Count;
                var q = from n in referencedProductions.SelectMany(p => p.Rhs).OfType<Nonterminal>()
                        join p in Productions on n equals p.Lhs
                        select p;
                referencedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
            } while(count < referencedProductions.Count);
            var unreferencedProductions = Productions.Except(referencedProductions).ToList();
            if(unreferencedProductions.Any())
            {
                // Issue a warning if any.  Don't include them in the augmented grammar.
                Console.Error.WriteLine("warning: {0} unreferenced productions:", unreferencedProductions.Count);
                foreach(var unreferencedProduction in unreferencedProductions)
                {
                    Console.Error.WriteLine(unreferencedProduction);
                }
            }

            // Create the augmented grammar (p. 222).
            var augmentedGrammar = new Grammar { Productions = new List<Production>() };
            augmentedGrammar.Productions.Add(new Production(Nonterminal.AugmentedStart, new[] { Productions[0].Lhs }));
            augmentedGrammar.Productions.AddRange(referencedProductions);

            // Algorithm 4.9, p. 231
            var items = augmentedGrammar.Items().Select((s, i) => new { Set = s, Index = i }).ToDictionary(p => p.Set, p => p.Index);

            // Create the action table.
            var a = from p in items
                    let x = from g in p.Key.Gotos
                            let t = g.Key as Terminal
                            where t != null
                            select new Entry { Terminal = t, Action = Action.Shift, Index = items[g.Value] }
                    let y = from i in p.Key.AsQueryable()
                            where i.DotPosition == augmentedGrammar.Productions[i.ProductionIndex].Rhs.Count
                            let t = i.Lookahead
                            let s = i.ProductionIndex == 0
                            where !s || t == Terminal.AugmentedEnd
                            select new Entry { Terminal = t, Action = s ? Action.Accept : Action.Reduce, Index = i.ProductionIndex }
                    select x.Concat(y);
            // TODO:  this does not account for conflicts.
            var actions = a.Select(e => e.ToDictionary(p => p.Terminal)).ToList();

            // Create the goto table.
            var c = from p in items
                    select from g in p.Key.Gotos
                           let n = g.Key as Nonterminal
                           where n != null
                           select new { Nonterminal = n, Target = items[g.Value] };
            // TODO:  this does not account for conflicts.
            var gotos = c.Select(e => e.ToDictionary(p => p.Nonterminal, p => p.Target)).ToList();

            return new KeyValuePair<List<Dictionary<Terminal, Entry>>, List<Dictionary<Nonterminal, int>>>(actions, gotos);
        }

        // closure(I), p. 232
        private Item.Set Closure(Item.Set items, IEnumerable<Production> expandedProductions)
        {
            int count;
            do
            {
                count = items.Count;

                Func<int, Production> fn1 = i =>
                {
                    return Productions[i];
                };
                Func<Production, Item, Nonterminal> fn2 = (p, i) =>
                {
                    return p.Rhs[i.DotPosition] as Nonterminal;
                };
                Func<Production, Nonterminal, bool> fn3 = (p, n) =>
                {
                    return p.Lhs == n;
                };

                // for each item [A → α∙Bβ, a] in I,
                // each production B → γ in G',
                // and each terminal b in FIRST(βa)
                // such that [B → ∙γ, b] is not in I do
                var q = from i in items.AsQueryable()
                        let ip = fn1(i.ProductionIndex)
                        where i.DotPosition < ip.Rhs.Count
                        let n = fn2(ip, i)
                        where n != null
                        let r = ip.Rhs.Skip(i.DotPosition + 1)
                        let l = i.Lookahead
                        from p in Productions.Select((x, y) => new { Production = x, Index = y })
                        where fn3(p.Production, n)
                        from b in First(r.Concat(new[] { l }), expandedProductions)
                        select new Item(p.Index, 0, b);

                // add [B → ∙γ, b] to I;
                items.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.

                // until no more items can be added to I;
            } while(count < items.Count);

            // return I
            return items;
        }

        // goto(I, X), p. 232
        private Item.Set Goto(Item.Set items, Symbol symbol, IEnumerable<Production> expandedProductions)
        {
            // let J be the set of items [A → αX∙β, a] such that
            // [A → α∙Xβ, a] is in I;
            var q = from i in items.AsQueryable()
                    let p = Productions[i.ProductionIndex]
                    let d = i.DotPosition
                    where d < p.Rhs.Count && p.Rhs[d] == symbol
                    select new Item(i.ProductionIndex, d + 1, i.Lookahead);

            // return closure(J)
            return Closure(new Item.Set(q), expandedProductions);
        }

        // items(G'), p. 232
        private List<Item.Set> Items()
        {
            // Create a collection of symbols used in the grammar.
            var symbols = new HashSet<Symbol>(Productions.SelectMany(p => p.Rhs));

            // Create a collection of terminals.
            var terminals = new HashSet<Terminal>(Productions.SelectMany(p => p.Rhs.OfType<Terminal>()));

            // Collect the expanded productions.
            var expandedProductions = CollectExpandedProductions(Productions);

            // Create a list to hold the items added to the closure.  Their
            // indicies will be the state indicies.
            var items = new List<Item.Set>();

            // C := {closure({[S' → ∙S, $]})};
            var c = new HashSet<Item.Set>(new[] { Closure(new Item.Set(new[] { new Item(0, 0, Terminal.AugmentedEnd) }), expandedProductions) });
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
                        var g = Goto(itemSet, symbol, expandedProductions);
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

        private static HashSet<Terminal> First(IEnumerable<Symbol> symbols, IEnumerable<Production> expandedProductions)
        {
            var first = new HashSet<Terminal>();

            foreach(var symbol in symbols)
            {
                var symbolFirst = First(symbol, expandedProductions);
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
        private static HashSet<Terminal> First(Symbol symbol, IEnumerable<Production> expandedProductions)
        {
            var first = new HashSet<Terminal>();

            if(symbol is Terminal)
            {
                first.Add((Terminal)symbol);
            }
            else
            {
                var q = from p in expandedProductions
                        where p.Lhs == symbol
                        select p.Rhs.FirstOrDefault() ?? Terminal.Epsilon;
                first.UnionWith(q.OfType<Terminal>());
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
                            select new Production(p.Lhs, p.Rhs.Skip(1));
                    expandedProductions.UnionWith(q.ToList()); // Use ToList to prevent an iteration exception.
                }

            } while(count < expandedProductions.Count);

            return expandedProductions;
        }

        public class Entry
        {
            public Terminal Terminal { get; set; }
            public Action Action { get; set; }
            public int Index { get; set; }
        }

        public enum Action { None, Shift, Reduce, Accept };
    }
}
