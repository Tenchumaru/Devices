﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pard
{
    // LR(1) item, p. 230
    class Item
    {
        public readonly int ProductionIndex;
        public readonly int DotPosition;
        public readonly Terminal Lookahead;
        private string name = "";

        public Item(int productionIndex, int dotPosition, Terminal lookahead)
        {
            ProductionIndex = productionIndex;
            DotPosition = dotPosition;
            Lookahead = lookahead;
            name = String.Format("{0} @ {1}, {2}", productionIndex, dotPosition, lookahead.Name);
        }

        public override bool Equals(object obj)
        {
            var that = obj as Item;
            return !Object.ReferenceEquals(that, null) && ToString() == that.ToString();
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override string ToString()
        {
            return name;
        }

        public static bool operator ==(Item left, Item right)
        {
            return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(right, null) : left.Equals(right);
        }

        public static bool operator !=(Item left, Item right)
        {
            return !(left == right);
        }

        internal class Set
        {
            public int Count { get { return set.Count; } }
            public readonly Dictionary<Symbol, Set> Gotos = new Dictionary<Symbol, Set>();

            private string name = "";
            private HashSet<Item> set;

            public override bool Equals(object obj)
            {
                var that = obj as Set;
                return !Object.ReferenceEquals(that, null) && ToString() == that.ToString();
            }

            public override int GetHashCode()
            {
                return name.GetHashCode();
            }

            public override string ToString()
            {
                return name;
            }

            public static bool operator ==(Set left, Set right)
            {
                return Object.ReferenceEquals(left, null) ? Object.ReferenceEquals(right, null) : left.Equals(right);
            }

            public static bool operator !=(Set left, Set right)
            {
                return !(left == right);
            }

            internal Set(IEnumerable<Item> items)
            {
                set = new HashSet<Item>(items);
                if(set.Any())
                {
                    // Use kernel items (the first item, which has the
                    // augmented start symbol, and any item whose dot position
                    // is greater than zero) for the name of this set.
                    var q = from i in set
                            where i.ProductionIndex == 0 || i.DotPosition > 0
                            select i.name;
                    name = String.Join("; ", q);
                }
            }

            internal bool Any()
            {
                return set.Any();
            }

            internal IQueryable<Item> AsQueryable()
            {
                return set.AsQueryable<Item>();
            }

            internal void UnionWith(IEnumerable<Item> items)
            {
                set.UnionWith(items);
            }
        }
    }
}