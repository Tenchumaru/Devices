﻿using System.Collections;
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
		public override string ToString() => "(epsilon)";
	}

	public abstract class ConcreteSymbol : Symbol {
		public virtual int Order => throw new NotImplementedException();
		public int SaveForAcceptance { get; set; }
		private static readonly Dictionary<char, char> knownEscapes = new() {
			{'\a', 'a' },
			{'\b', 'b' },
			{'\f', 'f' },
			{'\n', 'n' },
			{'\r', 'r' },
			{'\t', 't' },
			{'\v', 'v' },
		};

		public static ConcreteSymbol? operator -(ConcreteSymbol left, ConcreteSymbol right) {
			var rv = left.Difference(right);
			rv?.UpdateSaveForAcceptance(left, right);
			return rv;
		}

		public static ConcreteSymbol? operator &(ConcreteSymbol left, ConcreteSymbol right) {
			var rv = left.Intersect(right);
			rv?.UpdateSaveForAcceptance(left, right);
			return rv;
		}

		private void UpdateSaveForAcceptance(ConcreteSymbol left, ConcreteSymbol right) {
			if (left.SaveForAcceptance != 0 && right.SaveForAcceptance != 0) {
				if (left.SaveForAcceptance != right.SaveForAcceptance) {
					throw new ApplicationException("differing saves");
				}
				SaveForAcceptance = left.SaveForAcceptance;
			} else {
				SaveForAcceptance = left.SaveForAcceptance + right.SaveForAcceptance;
			}
		}

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
		public virtual string InternalToString() => throw new NotImplementedException();

		public virtual bool IsIn(Symbol that) => false;
		public override string ToString() {
			return SaveForAcceptance == 0 ? InternalToString() : $"{InternalToString()} (save {SaveForAcceptance})";
		}
	}

	public class AcceptingSymbol : ConcreteSymbol {
		public int Value { get; }

		public AcceptingSymbol(int value) => Value = value;

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			Debug.Assert(Value != (that as AcceptingSymbol)?.Value);
			return null;
		}

		public override string InternalToString() => $"(accept {Value})";
	}

	public class AnySymbol : ConcreteSymbol {
		public override int Order => int.MaxValue;
		public static readonly AnySymbol Value = new();

		private AnySymbol() { }

		public override bool Equals(Symbol? other) => ReferenceEquals(this, other);

		public override string InternalToString() => "(any)";

		public override string MakeExpression(string name) => "true";

		public override bool IsIn(Symbol that) {
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

		public override bool IsIn(Symbol that) {
			return that is BolSymbol;
		}

		public override string MakeExpression(string name) {
			return "reader_.IsAtBol";
		}

		public override bool Equals(Symbol? other) => ReferenceEquals(this, other);

		public override string InternalToString() => "(bol)";
	}

	public class EofSymbol : ConcreteSymbol {
		public override int Order => 3;
		public static readonly EofSymbol Value = new();

		private EofSymbol() { }

		public override ConcreteSymbol? Intersect(ConcreteSymbol that) {
			return that as EofSymbol;
		}

		public override bool IsIn(Symbol that) {
			return that is EofSymbol;
		}

		public override string MakeExpression(string name) {
			return $"{name}==-1";
		}

		public override bool Equals(Symbol? other) => ReferenceEquals(this, other);

		public override string InternalToString() => "(eof)";
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

		public override string InternalToString() {
			if (includedCharacters[0]) {
				BitArray ba = new(includedCharacters);
				RangeSymbol complement = new(ba.Not());
				var s = complement.ToString();
				return $"[^{s[1..]}";
			}
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
			StringBuilder sb = new(includedCharacters[0] ? "((" : "(");
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
			if (includedCharacters[0]) {
				sb.Append(")&&").Append(name).Append(">=0");
			}
			return sb.ToString();
		}

		public bool Includes(char value) => includedCharacters[value];

		public override bool IsIn(Symbol that) {
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

		public override string InternalToString() => $"'{Escape(Value)}'";

		public override string MakeExpression(string name) => $"{name}=={InternalToString()}";

		public override bool IsIn(Symbol that) {
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
