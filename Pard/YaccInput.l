using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pard
{
	public partial class YaccInput
	{
		partial class Scanner
		{
			private ScannerToken ReadSectionOne()
			{
				//** let ws = [\a\b\f\n\r\t\v ]

				//** %define
				return ReadRestOfLine(PDefine);

				//** %using
				return ReadRestOfLine(PUsing);

				//** %left
				return new ScannerToken { Symbol = PToken, Value = Associativity.Left };

				//** %right
				return new ScannerToken { Symbol = PToken, Value = Associativity.Right };

				//** %nonassoc
				return new ScannerToken { Symbol = PToken, Value = Associativity.Nonassociative };

				//** %token
				return new ScannerToken { Symbol = PToken, Value = Associativity.None };

				//** %type
				return new ScannerToken { Symbol = PType };

				//** [A-Za-z_][0-9A-Za-z_]*
				return new ScannerToken { Symbol = Identifier, Value = yy.TokenValue };

				//** %%{ws}+
				mode = ScannerMode.SectionTwo;
				return new ScannerToken { Symbol = PP };

				//** '\\
				{
					char value = ReadEscapedValue();
					return MakeLiteral(value);
				}

				//** '
				{
					var value = (char)yy.Get();
					return MakeLiteral(value);
				}

				//** :|<|>
				return new ScannerToken { Symbol = yy.TokenValue[0] };

				//** //[^\r\n]*
				// Ignore remarks.

				//** /\*
				// Ignore comments.
				CollectComments(false);

				//** {ws}+
				// Ignore white space.

				//**
				if(yy.ScanValue >= 0)
					ReportError("unexpected input: " + (char)yy.ScanValue);
				else
					ReportError("unexpected end of input");
				return ScannerToken.End;
			}

			private ScannerToken ReadSectionTwo()
			{
				//** let ws = [\a\b\f\n\r\t\v ]

				//** %prec
				return new ScannerToken { Symbol = PPrec };

				//** error
				return new ScannerToken { Symbol = ErrorToken };

				//** [A-Za-z_][0-9A-Za-z_]*
				return new ScannerToken { Symbol = Identifier, Value = yy.TokenValue };

				//** '\\
				{
					char value = ReadEscapedValue();
					return MakeLiteral(value);
				}

				//** '
				{
					var value = (char)yy.Get();
					return MakeLiteral(value);
				}

				//** \{
				currentAction.Length = 0;
				currentAction.Append(yy.TokenValue);
				return ReadSectionTwoAction();

				//** //[^\n]*
				// Ignore remarks.

				//** /\*
				// Ignore comments.
				CollectComments(false);

				//** {ws}+
				// Ignore white space.

				//** .
				return new ScannerToken { Symbol = yy.TokenValue[0] };

				//**
				return ScannerToken.End;
			}

			private ScannerToken ReadSectionTwoAction()
			{
				for(int scopeLevel = 1;;)
				{
					//** \"([^\\"]|(\\.))*\"
					// TODO: handle octal, hex, and Unicode escapes.
					currentAction.Append(yy.TokenValue);

					//** '([^\\']|(\\.))+'
					// TODO: handle octal, hex, and Unicode escapes.
					currentAction.Append(yy.TokenValue);

					//** //[^\n]*
					currentAction.Append(yy.TokenValue);

					//** /\*
					currentAction.Append(yy.TokenValue);
					CollectComments(true);

					//** \{
					currentAction.Append('{');
					++scopeLevel;

					//** \}
					currentAction.Append('}');
					if(--scopeLevel == 0)
						return new ScannerToken { Symbol = CodeBlock, Value = currentAction.ToString() };

					//** \n
					currentAction.Append('\n');
					if(scopeLevel == 0)
						return new ScannerToken { Symbol = CodeBlock, Value = currentAction.ToString() };

					//** .
					currentAction.Append(yy.TokenValue[0]);

					//**
					if(scopeLevel == 0)
						return new ScannerToken { Symbol = CodeBlock, Value = currentAction.ToString() };
					ReportError("unmatched action braces at EOF");
					return ScannerToken.End;
				}
			}

			private char ReadEscapedValue()
			{
				//** let hex = [0-9A-Fa-f]
				//** u{hex}{4}
				{
					var value = int.Parse(yy.TokenValue, NumberStyles.AllowHexSpecifier);
					return (char)value;
				}
				//** x{hex}{2}
				{
					var value = int.Parse(yy.TokenValue, NumberStyles.AllowHexSpecifier);
					return (char)value;
				}
				//** 0[01]?[0-7]{0,5}
				{
					int value = 0;
					foreach(char ch in yy.TokenValue)
					{
						value *= 8;
						value += ch - '0';
					}
					return (char)value;
				}

				//** a
				return '\a';

				//** b
				return '\b';

				//** f
				return '\f';

				//** n|\n
				return '\n';

				//** r
				return '\r';

				//** t
				return '\t';

				//** v
				return '\v';

				//** .
				return yy.TokenValue[0];

				//**
				ReportError("unexpected end of input");
				return char.MaxValue;
			}

			private void CollectComments(bool isCollecting)
			{
				for(;;)
				{
					//** \*/
					if(isCollecting)
						currentAction.Append(yy.TokenValue);
					return;

					//** .|\n
					if(isCollecting)
						currentAction.Append(yy.TokenValue[0]);

					//**
					ReportError("unexpected end of input");
					return;
				}
			}
		}
	}
}
