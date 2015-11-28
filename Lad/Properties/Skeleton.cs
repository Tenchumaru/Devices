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
			private int scanValue;
#if TRACKING_LINE_NUMBER
			private int lineNumber;
#endif
#if USES_BOL
			private bool atBol = true;
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
				for(int i = marker; i < position; ++i)
				{
					if(buffer[i] == '\n')
						--lineNumber;
				}
#endif
#if USES_BOL
				atBol = position > 0 && buffer[position - 1] == '\n';
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
					atBol = false;
					return BOL;
				}
				int ch = PrivateGet();
				if(ch == '\n')
					atBol = true;
				return ch;
			}

			private int PrivateGet()
			{
#endif
				if(position >= buffer.Length)
				{
					if(scanValue < 0)
						return scanValue;
					int ch = reader.Read();
					if(ch < 0)
						return scanValue = -1;
#if TRACKING_LINE_NUMBER
					if(ch == '\n')
						++lineNumber;
#endif
					buffer.Append((char)ch);
				}
				++position;
				return scanValue = buffer[position - 1];
			}

			internal int Take()
			{
				int ch = Get();
				Save();
				Restore();
				return ch;
			}
#if TRACKING_LINE_NUMBER

			public int LineNumber
			{
				get { return lineNumber; }
			}
#endif
#if USES_REJECT

			internal void Reject()
			{
				scanner.wantsToReject = true;
			}
#endif

			internal string Text
			{
				get { return tokenValue; }
			}
		}

		public class EofEventArgs : EventArgs
		{
			public TextReader NewReader { get; set; }
		}

		public delegate void EofHandler(object sender, EofEventArgs e);

		public event EofHandler Eof;
		private YY yy;
#if USES_REJECT
		private bool wantsToReject;

		private class RejectState
		{
			internal RejectState(int state, int position)
			{
				State = state;
				Position = position;
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
				yy = new YY(e.NewReader);
				lastCh = yy.Get();
				if(lastCh != -1)
					return true;
			}
			return false;
		}

		public Token Read()
		{
#if USES_REJECT
			Stack<RejectState> acceptingStates = new Stack<RejectState>();
#endif
#if USES_TRAILING_CONTEXT
			trailingContextPositions.Initialize();
#endif
			; // state machine and rule actions $
			return null;
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
			var scanner = new Scanner(Console.In);
			scanner.Read();
		}

		internal class Token {}
#endif
	}
