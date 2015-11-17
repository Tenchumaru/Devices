using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

	class Scanner // using directives, namespace, and class declaration $
	{
		private class YY
		{
			private TextReader reader;
			private int marker, position;
			private StringBuilder buffer = new StringBuilder();
			private string tokenValue;
			private int ScanValue;
#if TRACKING_LINE_NUMBER
			private int lineNumber;
#endif
#if USES_BOL
			bool atBol= true;
#endif

			public YY(TextReader reader)
			{
				this.reader = reader;
			}

			internal void Save()
			{
				marker = position;
			}

			internal void Restore()
			{
#if TRACKING_LINE_NUMBER
				for(int i= marker; i < position; ++i)
				{
					if(buffer[i] == '\n')
						--lineNumber;
				}
#endif
#if USES_BOL
				atBol= position > 0 && buffer[position - 1] == '\n';
#endif
				tokenValue = buffer.ToString(0, marker);
				buffer.Remove(position = 0, marker);
				marker = 0;
			}

			internal int Get()
			{
#if USES_BOL
				if(atBol)
				{
					atBol= false;
					return BOL;
				}
				int ch= PrivateGet();
				if(ch == '\n')
					atBol= true;
				return ch;
			}

			private int PrivateGet()
			{
#endif
				if(position >= buffer.Length)
				{
					if(ScanValue < 0)
						return ScanValue;
					int ch = reader.Read();
					if(ch < 0)
						return ScanValue = -1;
#if TRACKING_LINE_NUMBER
					if(ch == '\n')
						++lineNumber;
#endif
					buffer.Append((char)ch);
				}
				++position;
				return ScanValue = buffer[position - 1];
			}

			internal int Take()
			{
				int ch = Get();
				Save();
				Restore();
				return ch;
			}
#if TRACKING_LINE_NUMBER

			internal int LineNumber
			{
				get { return lineNumber; }
			}
#endif
#if USES_RULE_GROUPS

			internal RuleGroup RuleGroup
			{
				get { return scanner.currentRuleGroup; }
				set { scanner.currentRuleGroup= value; }
			}
#endif
#if USES_REJECT

			internal void Reject()
			{
				scanner.wantsToReject= true;
			}
#endif

			internal string Text
			{
				get { return tokenValue; }
			}
		}

		public class EofEventArgs : EventArgs
		{
			private TextReader newReader;

			public TextReader NewReader
			{
				get { return newReader; }
				set { newReader= value; }
			}
		}

		public delegate void EofHandler(object sender, EofEventArgs e);

		public event EofHandler Eof;
		private TextReader reader;
		private YY yy;
		private string readerValue, savedValue;
		private bool wantsMore;
#if INVOKED_BY_PARSER
		private Token token;
#endif
#if USES_REJECT
		private bool wantsToReject;

		private class RejectState
		{
			internal RejectState(int state, int position)
			{
				State= state;
				Position= position;
			}

			internal int State, Position;
		}
#endif

		public Scanner(TextReader reader) // constructor definition $
		{
			yy = new YY(reader);
		}

		private bool CheckForMore(ref int lastCh)
		{
			for(EofHandler eof = Eof; eof != null; eof = Eof)
			{
				var e= new EofEventArgs();
				eof(this, e);
				if(e.NewReader == null)
					break;
				yy = new YY(reader);
				lastCh = yy.Get();
				if(lastCh != -1)
					return true;
			}
			return false;
		}

#if INVOKED_BY_PARSER
		public Token Scan(Union tokenValue)
#else
		public int Scan()
#endif
		{
#if USES_REJECT
			Stack<RejectState> acceptingStates= new Stack<RejectState>();
#endif
#if USES_TRAILING_CONTEXT
			trailingContextPositions.Initialize();
#endif
			if(wantsMore)
				savedValue += readerValue;
			else
				savedValue= "";
			; // state machine and rule actions $
#if INVOKED_BY_PARSER
			return token;
#else
			return ch_;
#endif
		}
#if TRACKING_LINE_NUMBER

		internal int LineNumber
		{
			get { return yy.LineNumber; }
		}
#endif
		// additional code (section three) $
#if WANTS_MAIN
		static void Main(string[] args)
		{
			var scanner= new Scanner(Console.In);
			scanner.Scan();
		}
#endif
	}
