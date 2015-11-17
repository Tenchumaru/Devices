using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lad
{
    public abstract class Symbol : IEquatable<Symbol>
    {
        public static bool operator ==(Symbol left, Symbol right)
        {
            if(object.ReferenceEquals(left, null))
                return object.ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Symbol left, Symbol right)
        {
            return !(left == right);
        }

        public virtual bool Equals(Symbol other)
        {
            return !object.ReferenceEquals(other, null) && GetType() == other.GetType() && ToString() == other.ToString();
        }

        public override bool Equals(object obj)
        {
            var that = obj as Symbol;
            return that != null && Equals(that);
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }

    public class EpsilonSymbol : Symbol
    {
        public static readonly EpsilonSymbol Value = new EpsilonSymbol();

        private EpsilonSymbol()
        {
        }

        public override bool Equals(Symbol other)
        {
            return object.ReferenceEquals(this, other);
        }

        public override string ToString()
        {
            return "(epsilon)";
        }
    }

    public abstract class ConcreteSymbol : Symbol
    {
        public static string Escape(char ch)
        {
            if(ch < ' ')
                return string.Format(@"\x{0:x2}", (int)ch);
            if(ch == '\'')
                return @"\'";
            if(ch == '\\')
                return @"\\";
            if(ch > '\xff')
                return string.Format(@"\u{0:x4}", (int)ch);
            if(ch > '~')
                return string.Format(@"\x{0:x}", (int)ch);
            return ch.ToString();
        }

        public virtual bool Contains(ConcreteSymbol symbol)
        {
            return false;
        }

        public virtual ConcreteSymbol Remove(IEnumerable<ConcreteSymbol> symbols)
        {
            throw new NotImplementedException();
        }

        public virtual void RemoveFrom(BitArray isIn)
        {
            throw new NotImplementedException();
        }
    }

    public class AnySymbol : ConcreteSymbol
    {
        public static readonly AnySymbol Value = new AnySymbol(true);
        public static readonly AnySymbol WithoutNewLine = new AnySymbol(false);
        private bool includesNewLine;

        private AnySymbol(bool includesNewLine)
        {
            this.includesNewLine = includesNewLine;
        }

        public override bool Contains(ConcreteSymbol symbol)
        {
            if(symbol == this || symbol == BolSymbol.Value)
                return false;
            if(includesNewLine)
                return true;
            var simple = symbol as SimpleSymbol;
            if(simple != null)
                return simple.Value != '\n';
            var range = (RangeSymbol)symbol;
            return !range.Contains(new SimpleSymbol('\n'));
        }

        public override ConcreteSymbol Remove(IEnumerable<ConcreteSymbol> symbols)
        {
            var range = new RangeSymbol(char.MinValue, char.MaxValue);
            if(!includesNewLine)
                range = (RangeSymbol)range.Remove(new[] { new SimpleSymbol('\n') });
            return range.Remove(symbols);
        }

        public override bool Equals(Symbol other)
        {
            return object.ReferenceEquals(this, other);
        }

        public override string ToString()
        {
            return includesNewLine ? "(any)" : "(any-nl)";
        }
    }

    public class BolSymbol : ConcreteSymbol
    {
        public static readonly BolSymbol Value = new BolSymbol();

        private BolSymbol()
        {
        }

        public override bool Equals(Symbol other)
        {
            return object.ReferenceEquals(this, other);
        }

        public override string ToString()
        {
            return "^";
        }
    }

    public class RangeSymbol : ConcreteSymbol
    {
        private BitArray isIn;

        public RangeSymbol(char first, char last)
        {
            isIn = new BitArray(char.MaxValue + 1);
            for(int i = first; i <= last; ++i)
                isIn[i] = true;
        }

        private RangeSymbol(BitArray isIn)
        {
            this.isIn = new BitArray(isIn);
        }

        public static RangeSymbol operator +(RangeSymbol left, RangeSymbol right)
        {
            return new RangeSymbol(new BitArray(left.isIn).Or(right.isIn));
        }

        public static RangeSymbol operator -(RangeSymbol range)
        {
            return new RangeSymbol(new BitArray(range.isIn).Not());
        }

        public List<KeyValuePair<char, char>> ComposeSubRanges()
        {
            var subRanges = new List<KeyValuePair<char, char>>();
            char? first = null;
            for(int i = 0; i < isIn.Length; ++i)
            {
                if(isIn[i] && first == null)
                    first = (char)i;
                else if(!isIn[i] && first != null)
                {
                    subRanges.Add(new KeyValuePair<char, char>(first.Value, (char)(i - 1)));
                    first = null;
                }
            }
            if(first != null)
                subRanges.Add(new KeyValuePair<char, char>(first.Value, char.MaxValue));
            return subRanges;
        }

        public bool Contains(SimpleSymbol symbol)
        {
            return isIn[symbol.Value];
        }

        public override bool Contains(ConcreteSymbol symbol)
        {
            if(symbol == this || symbol == BolSymbol.Value || symbol is AnySymbol)
                return false;
            if(symbol is SimpleSymbol)
                return this.Contains((SimpleSymbol)symbol);
            var rangeSymbol = (RangeSymbol)symbol;
            for(int i = 0; i < isIn.Length; ++i)
            {
                if(!isIn[i] && rangeSymbol.isIn[i])
                    return false;
            }
            return true;
        }

        public override ConcreteSymbol Remove(IEnumerable<ConcreteSymbol> symbols)
        {
            var isIn = new BitArray(this.isIn);
            foreach(ConcreteSymbol symbol in symbols)
                symbol.RemoveFrom(isIn);
            return isIn.Cast<bool>().Any(b => b) ? new RangeSymbol(isIn) : null;
        }

        public override void RemoveFrom(BitArray isIn)
        {
            var complement = new BitArray(this.isIn).Not();
            isIn.And(complement);
        }

        public override string ToString()
        {
            var sb = new StringBuilder("[");
            foreach(var pair in ComposeSubRanges())
                sb.AppendFormat("{0}-{1}", Escape(pair.Key), Escape(pair.Value));
            return sb.Append(']').ToString();
        }
    }

    public class SimpleSymbol : ConcreteSymbol
    {
        public char Value { get; private set; }

        public SimpleSymbol(char ch)
        {
            Value = ch;
        }

        public override void RemoveFrom(BitArray isIn)
        {
            isIn.Set(Value, false);
        }

        public override bool Equals(Symbol other)
        {
            return !object.ReferenceEquals(other, null) && other.GetType() == typeof(SimpleSymbol) && Value == ((SimpleSymbol)other).Value;
        }

        public override string ToString()
        {
            return string.Format("'{0}'", Escape(Value));
        }
    }
}
