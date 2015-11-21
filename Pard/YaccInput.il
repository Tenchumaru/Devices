using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pard
{
	public partial class YaccInput
	{
		partial class Scanner
		{
			private Token ReadSectionOne()
			{
				//** let ws = [\a\b\f\n\r\t\v ]

				//** %define
				return ReadRestOfLine(PDefine);

				//** %using
				return ReadRestOfLine(PUsing);

				//** %left
				return new Token { Symbol = PToken, Value = Grammar.Associativity.Left };

				//** %right
				return new Token { Symbol = PToken, Value = Grammar.Associativity.Right };

				//** %nonassoc
				return new Token { Symbol = PToken, Value = Grammar.Associativity.Nonassociative };

				//** %token
				return new Token { Symbol = PToken, Value = Grammar.Associativity.None };

				//** %type
				return new Token { Symbol = PType };

				//** [A-Za-z_][0-9A-Za-z_]*
				return new Token { Symbol = Identifier, Value = yy.TokenValue };

				//** %%{ws}+
				mode = ScannerMode.SectionTwo;
				return new Token { Symbol = PP };

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
				return new Token { Symbol = yy.TokenValue[0] };

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
				return Token.End;
			}

			private Token ReadSectionTwo()
			{
				//** let ws = [\a\b\f\n\r\t\v ]

				//** %prec
				return new Token { Symbol = PPrec };

				//** error
				return new Token { Symbol = ErrorToken };

				//** [A-Za-z_][0-9A-Za-z_]*
				return new Token { Symbol = Identifier, Value = yy.TokenValue };

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
				return new Token { Symbol = yy.TokenValue[0] };

				//**
				return Token.End;
			}

			private Token ReadSectionTwoAction()
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
						return new Token { Symbol = CodeBlock, Value = currentAction.ToString() };

					//** \n
					currentAction.Append(Environment.NewLine);
					if(scopeLevel == 0)
						return new Token { Symbol = CodeBlock, Value = currentAction.ToString() };

					//** .
					currentAction.Append(yy.TokenValue[0]);

					//**
					if(scopeLevel == 0)
						return new Token { Symbol = CodeBlock, Value = currentAction.ToString() };
					ReportError("unmatched action braces at EOF");
					return Token.End;
				}
			}

			private char ReadEscapedValue()
			{
				//** let hex = [0-9A-Fa-f]
				//** u{hex}{4}
				{
					var value = Int32.Parse(yy.TokenValue, NumberStyles.AllowHexSpecifier);
					return (char)value;
				}
				//** x{hex}{2}
				{
					var value = Int32.Parse(yy.TokenValue, NumberStyles.AllowHexSpecifier);
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
