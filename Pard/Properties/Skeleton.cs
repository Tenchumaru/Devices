using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pair_ = System.Collections.Generic.KeyValuePair<int, object>;
	class Skeleton // using directives, namespace, and class $
	{
		public event EventHandler<ParseErrorEventArgs> Error;
		private IScanner scanner; // scanner field $

		public Skeleton(IScanner scanner) // constructor $
		{
			this.scanner = scanner;
		}

		public bool Parse()
		{
			int state_ = 0;
			var stack_ = new List<Pair_> { new Pair_(state_, null) };
			var gotos_ = new int[,] {
			// gotos $
			};
			var reductions_ = new[] {
			// reductions $
			};
			var token_ = scanner.Read();
			var reduction_ = new R_();
			object reductionValue_ = null;
			for(; ; )
			{
				switch(state_)
				{
				case 0: break; // transitions $
				case -1:
					if(reduction_.Count > 0)
						reductionValue_ = stack_[stack_.Count - reduction_.Count].Value;
					goto reduce2;
#pragma warning disable 162 // unreachable code
				case 1: break; // actions $
#pragma warning restore 162
shift:
					stack_.Add(new Pair_(state_, token_.Value));
token_ = scanner.Read();
continue;
reduce1:
reduction_ = reductions_[state_];
state_ = reduction_.Action;
reductionValue_ = null;
continue;
reduce2:
int count = reduction_.Count;
stack_.RemoveRange(stack_.Count - count, count);
state_ = gotos_[stack_.Last().Key, reduction_.Symbol];
stack_.Add(new Pair_(state_, reductionValue_));
continue;
				}
				if(stack_.Count > 1)
				{
					switch(token_.Symbol)
					{
					case -2:
						stack_.RemoveRange(stack_.Count - 1, 1);
						state_ = stack_.Last().Key;
						break;
					case -1:
						return false;
					default:
						bool? result = HandleError(token_.Symbol, token_.Value);
						if(result.HasValue)
							return result.Value;
						token_.Symbol = -2;
						break;
					}
				}
				else
					return false;
			}
		}
		private bool? HandleError(int symbol, object value)
		{
			var eventArgs = new ParseErrorEventArgs(symbol, value);
			if(Error != null)
				Error(this, eventArgs);
			return eventArgs.Result;
		}
		// terminal definitions $
		struct R_
		{
			public readonly int Symbol, Count, Action;

			public R_(int symbol, int count, int action)
			{
				Count = count;
				Symbol = symbol;
				Action = action;
			}

			public R_(int symbol, int count)
				: this(symbol, count, -1)
			{
			}
		}
		public class ParseErrorEventArgs : EventArgs
		{
			public readonly int Symbol;
			public readonly object Value;
			public bool? Result;
			public ParseErrorEventArgs(int symbol, object value)
			{
				Symbol = symbol;
				Value = value;
			}
		}
	}
