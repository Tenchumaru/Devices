using System.Collections;
using System.Diagnostics;
using System.Text;

namespace Lad {
	public abstract class Symbol : IEquatable<Symbol> {
		public virtual Symbol MakeDegenerate() => this;
		public static bool operator ==(Symbol? left, Symbol? right) => left is null ? right is null : left.Equals(right);

		public static bool operator !=(Symbol? left, Symbol? right) => !(left == right);

		public virtual bool Equals(Symbol? other) => other is not null && GetType() == other.GetType() && ToString() == other.ToString();

		public override bool Equals(object? obj) {
			Symbol? that = obj as Symbol;
			return that is not null && Equals(that);
		}

		public override int GetHashCode() => ToString().GetHashCode();

		public abstract override string ToString();
	}

	public class EpsilonSymbol : Symbol {
		public bool IsSavePoint { get; set; }
		public int SaveForAcceptance { get; set; }

		public override string ToString() => IsSavePoint ? $"(save {SaveForAcceptance})" : "(epsilon)";
	}

	public abstract class ConcreteSymbol : Symbol {
		public virtual int Order => throw new NotImplementedException();
		private static readonly Dictionary<char, char> knownEscapes = new() {
			{'\a', 'a' },
			{'\b', 'b' },
			{'\f', 'f' },
			{'\n', 'n' },
			{'\r', 'r' },
			{'\t', 't' },
			{'\v', 'v' },
		};

		public static ConcreteSymbol? operator -(ConcreteSymbol left, ConcreteSymbol right) => left.Difference(right);

		public static ConcreteSymbol? operator &(ConcreteSymbol left, ConcreteSymbol right) => left.Intersect(right);

		public virtual ConcreteSymbol? Intersect(ConcreteSymbol that) => throw new NotImplementedException();

		public virtual ConcreteSymbol? Difference(ConcreteSymbol that) => throw new NotImplementedException();

		public static string Escape(char ch) {
			if (ch < ' ') {
				if (knownEscapes.TryGetValue(ch, out char escapedCh)) {
					return $@"\{escapedCh}";
				}
				return $@"\x{(int)ch:x2}";
			} else if (ch == '\'') {
				return @"\'";
			} else if (ch == '\\') {
				return @"\\";
			} else if (ch > '\x7f') {
				return $@"\u{(int)ch:x4}";
			} else if (ch > '~') {
				return $@"\x{(int)ch:x}";
			}
			return ch.ToString();
		}

		public static string Escape(int ch) => Escape((char)ch);

		public virtual string MakeExpression(string name) => throw new NotImplementedException();

		internal virtual bool IsIn(Symbol that) => false;
	}

	public class AcceptingSymbol : ConcreteSymbol {
		public int Value { get; }

		public AcceptingSymbol(int value) => Value = value;

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			Debug.Assert(Value != (that as AcceptingSymbol)?.Value);
			return null;
		}

		public override string ToString() => $"(accept {Value})";
	}

	public class AnySymbol : ConcreteSymbol {
		public override int Order => 3;
		public static readonly AnySymbol Value = new();

		private AnySymbol() { }

		public override bool Equals(Symbol? other) => ReferenceEquals(this, other);

		public override string ToString() => "(any)";

		public override string MakeExpression(string name) => "true";

		internal override bool IsIn(Symbol that) {
			if (this == that) {
				return true;
			} else if (that is RangeSymbol range) {
				RangeSymbol anyRange = new(char.MinValue, char.MaxValue);
				return anyRange.IsIn(range);
			}
			return false;
		}

		public override ConcreteSymbol? Difference(ConcreteSymbol that) {
			if (this == that) {
				return this;
			}
			RangeSymbol range = new(char.MinValue, char.MaxValue);
			return range.Difference(that);
		}

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			if (this == that) {
				return this;
			}
			RangeSymbol range = new(char.MinValue, char.MaxValue);
			return range.Intersect(that);
		}
	}

	public class BolSymbol : ConcreteSymbol {
		public override int Order => 0;
		public static readonly BolSymbol Value = new();

		private BolSymbol() { }

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			return that as BolSymbol;
		}

		internal override bool IsIn(Symbol that) {
			return that is BolSymbol;
		}

		public override string MakeExpression(string name) {
			return "reader_.IsAtBol";
		}

		public override bool Equals(Symbol? other) => ReferenceEquals(this, other);

		public override string ToString() => "(bol)";
	}

	public class RangeSymbol : ConcreteSymbol {
		public override int Order => 2;
		private readonly BitArray includedCharacters = new(char.MaxValue + 1);

		public RangeSymbol(char first, char last) {
			if (last < first) {
				for (int i = last; i <= first; ++i) {
					includedCharacters[i] = true;
				}
			} else {
				for (int i = first; i <= last; ++i) {
					includedCharacters[i] = true;
				}
			}
		}

		public RangeSymbol(char ch) {
			includedCharacters[ch] = true;
		}

		private RangeSymbol(BitArray includedCharacters) => this.includedCharacters = includedCharacters;

		public static RangeSymbol operator +(RangeSymbol left, RangeSymbol right) => new(left.includedCharacters.Or(right.includedCharacters));

		public static RangeSymbol operator ~(RangeSymbol range) => new(range.includedCharacters.Not());

		public override string ToString() {
			StringBuilder sb = new("[");
			for (int i = 1; i < includedCharacters.Length; ++i) {
				if (!includedCharacters[i - 1] && includedCharacters[i] && (i + 1 == includedCharacters.Length || !includedCharacters[i + 1])) {
					sb.Append(Escape(i));
					++i;
				} else if (includedCharacters[i - 1] != includedCharacters[i]) {
					if (includedCharacters[i]) {
						sb.Append(Escape(i));
					} else {
						sb.Append($"-{Escape(i - 1)}");
					}
				}
			}
			if (includedCharacters[includedCharacters.Count - 1]) {
				sb.Append('-');
			}
			return sb.Append(']').ToString();
		}

		public override Symbol MakeDegenerate() {
			Degenerator degenerator = new(includedCharacters);
			if (degenerator.AreAllTrue) {
				return AnySymbol.Value;
			} else if (degenerator.AreAllFalse) {
				return new EpsilonSymbol();
			} else if (degenerator.Index > -1) {
				return new SimpleSymbol((char)degenerator.Index);
			}
			return this;
		}

		public override string MakeExpression(string name) {
			StringBuilder sb = new("(");
			for (int i = 1; i < includedCharacters.Length; ++i) {
				if (includedCharacters[i - 1] != includedCharacters[i]) {
					if (!includedCharacters[i - 1] && includedCharacters[i] && (i + 1 == includedCharacters.Length || !includedCharacters[i + 1])) {
						sb.AppendFormat("{0}=='{1}')||(", name, Escape(i));
						++i;
					} else if (includedCharacters[i]) {
						sb.AppendFormat("{0}>'{1}'&&", name, Escape(i - 1));
					} else {
						sb.AppendFormat("{0}<'{1}')||(", name, Escape(i));
					}
				}
			}
			if (sb.Length == 1) {
				return includedCharacters[0] ? "true" : "false";
			} else if (sb[^1] == '&') {
				sb[^2] = ')';
				--sb.Length;
			} else {
				sb.Length -= 3;
			}
			return sb.ToString();
		}

		internal bool Includes(char value) => includedCharacters[value];

		internal override bool IsIn(Symbol that) {
			if (that == AnySymbol.Value) {
				return true;
			} else if (that is RangeSymbol range) {
				BitArray combined = new(includedCharacters);
				combined = combined.And(range.includedCharacters);
				return combined.Cast<bool>().SequenceEqual(includedCharacters.Cast<bool>());
			} else if (that is SimpleSymbol simple) {
				if (!includedCharacters[simple.Value]) {
					return false;
				}
				return includedCharacters.Cast<bool>().Select((b, i) => new { b, i }).All(a => !a.b || a.i == simple.Value);
			}
			return false;
		}

		public override ConcreteSymbol? Difference(ConcreteSymbol that) {
			if (that == AnySymbol.Value) {
				return null;
			} else if (that is RangeSymbol range) {
				BitArray ba = new(includedCharacters.Cast<bool>().Zip(range.includedCharacters.Cast<bool>()).Select(a => a.First && !a.Second).ToArray());
				if (ba.Cast<bool>().All(b => !b)) {
					return null;
				}
				Symbol symbol = new RangeSymbol(ba).MakeDegenerate();
				return symbol as ConcreteSymbol;
			} else if (that is SimpleSymbol simple) {
				if (includedCharacters[simple.Value]) {
					BitArray ba = new(includedCharacters);
					ba[simple.Value] = false;
					Symbol symbol = new RangeSymbol(ba).MakeDegenerate();
					return symbol as ConcreteSymbol;
				}
				return this;
			}
			throw new NotImplementedException();
		}

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			if (that == AnySymbol.Value) {
				return this;
			} else if (that is RangeSymbol range) {
				BitArray ba = new(includedCharacters.Cast<bool>().Zip(range.includedCharacters.Cast<bool>()).Select(a => a.First && a.Second).ToArray());
				if (ba.Cast<bool>().All(b => !b)) {
					return null;
				}
				Symbol symbol = new RangeSymbol(ba).MakeDegenerate();
				return symbol as ConcreteSymbol;
			} else if (that is SimpleSymbol simple) {
				return includedCharacters[simple.Value] ? simple : null;
			}
			return that.Intersect(this);
		}

		class Degenerator {
			public bool AreAllFalse { get; private set; } = true;
			public bool AreAllTrue { get; private set; } = true;
			public int Index { get; private set; } = -1;

			public Degenerator(BitArray bitArray) {
				foreach ((bool bit, int index) in bitArray.Cast<bool>().Select((b, i) => (b, i))) {
					if (bit) {
						if (AreAllFalse) {
							Index = index;
						} else if (!AreAllTrue) {
							Index = -1;
							return;
						}
						AreAllFalse = false;
					} else {
						AreAllTrue = false;
					}
				}
			}
		}
	}

	public class SimpleSymbol : ConcreteSymbol {
		public override int Order => 1;
		public char Value { get; private set; }

		public SimpleSymbol(char ch) => Value = ch;

		public override string ToString() => $"'{Escape(Value)}'";

		public override string MakeExpression(string name) => $"{name}=={this}";

		internal override bool IsIn(Symbol that) {
			if (that == AnySymbol.Value) {
				return true;
			} else if (that is RangeSymbol range) {
				return range.Includes(Value);
			} else if (that is SimpleSymbol simple) {
				return Value == simple.Value;
			}
			return false;
		}

		public override ConcreteSymbol? Difference(ConcreteSymbol that) {
			if (that == AnySymbol.Value) {
				return null;
			} else if (that is RangeSymbol range) {
				return range.Includes(Value) ? null : this;
			} else if (that is SimpleSymbol simple) {
				return Value == simple.Value ? null : this;
			}
			throw new NotImplementedException();
		}

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			if (that is SimpleSymbol simple) {
				return Value == simple.Value ? this : null;
			}
			return that.Intersect(this);
		}
	}
}
