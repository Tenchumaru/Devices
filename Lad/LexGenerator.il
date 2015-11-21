using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Lad
{
	partial class LexGenerator
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

				//** %option
				return new Token { Symbol = POption };

				//** default
				return new Token { Symbol = Default };

				//** [a-z]+
				return new Token { Symbol = OptionValue, Value = tokenValue };

				//** %%{ws}+
				mode = ScannerMode.SectionTwoExpression;
				return new Token { Symbol = PP };

				//** {ws}+
				// Ignore white space.

				//**
				return HandleNoMatch();
			}

			private Token ReadSectionTwoExpression()
			{
				//** let number = [0-9]+

				//** [\a\b\f\r\t\v ]+
				return ReadSectionTwoAction();

				//** \n
				ReportError("expression without action");
				return Token.End;

				//** \[
				mode = ScannerMode.SectionTwoClass;
				return new Token { Symbol = ScanValue };

				//** \[^
				mode = ScannerMode.SectionTwoClass;
				return new Token { Symbol = NegativeClass };

				//** \{{number},{number}\}
				{
					string[] parts = tokenValue.Split('{', ',', '}');
					var min = int.Parse(parts[1]);
					var max = int.Parse(parts[2]);
					if(max < min || max == 0)
						ReportError("invalid range {0} - {1}", min, max);
					var value = new KeyValuePair<int, int>(min, max);
					return new Token { Symbol = Double, Value = value };
				}

				//** \{{number}\}
				{
					string[] parts = tokenValue.Split('{', '}');
					var value = int.Parse(parts[1]);
					if(value == 0)
						ReportError("invalid zero count");
					return new Token { Symbol = Single, Value = value };
				}

				//** \{[A-Za-z_][0-9A-Za-z_]*\}
				{
					string[] parts = tokenValue.Split('{', '}');
					return new Token { Symbol = NamedExpression, Value = parts[1] };
				}

				//** \\
				return new Token { Symbol = Symbol, Value = ReadEscapedValue() };

				//** \"
				mode = ScannerMode.SectionTwoQuotedExpression;
				return ReadSectionTwoQuotedExpression();

				//**
				return HandleNoMatch();
			}

			private Token ReadSectionTwoClass()
			{
				//** \]
				mode = ScannerMode.SectionTwoExpression;
				return new Token { Symbol = ScanValue };

				//** \\
				return new Token { Symbol = Symbol, Value = ReadEscapedValue() };

				//** -
				return new Token { Symbol = ScanValue };

				//** \n
				ReportError("unbalanced brackets");
				return Token.End;

				//**
				return new Token { Symbol = Symbol, Value = Take() };
			}

			private Token ReadSectionTwoQuotedExpression()
			{
				//** \"
				mode = ScannerMode.SectionTwoExpression;
				return ReadSectionTwoExpression();

				//** \\
				return new Token { Symbol = Symbol, Value = ReadEscapedValue() };

				//** \n
				ReportError("unterminated string constant");
				return Token.End;

				//**
				return new Token { Symbol = Symbol, Value = Take() };
			}

			private StringBuilder currentAction = new StringBuilder();
			private Token ReadSectionTwoAction()
			{
				currentAction.Length = 0;
				for(int scopeLevel = 0;;)
				{
					//** \"([^"]|(\\.))*"
					// TODO: handle octal, hex, and Unicode escapes.
					currentAction.Append(tokenValue);

					//** \{
					currentAction.Append('{');
					++scopeLevel;

					//** \}
					currentAction.Append('}');
					if(scopeLevel == 0)
					{
						ReportError("unmatched action braces");
						return Token.End;
					}
					--scopeLevel;

					//** \|[\a\b\f\n\r\t\v ]*
					if(currentAction.Length == 0)
						return new Token { Symbol = NextAction };
					currentAction.Append(tokenValue);

					//** \n
					currentAction.Append('\n');
					if(scopeLevel == 0)
						return new Token { Symbol = Action, Value = currentAction.ToString() };

					//**
					if(ScanValue < 0)
					{
						if(scopeLevel == 0)
							return new Token { Symbol = Action, Value = currentAction.ToString() };
						ReportError("unmatched action braces");
						return Token.End;
					}
					currentAction.Append((char)Take());
				}
			}

			private char ReadEscapedValue()
			{
				//** let hex = [0-9A-Fa-f]
				//** u{hex}{4}
				{
					var value = int.Parse(tokenValue, NumberStyles.AllowHexSpecifier);
					return (char)value;
				}
				//** x{hex}{2}
				{
					var value = int.Parse(tokenValue, NumberStyles.AllowHexSpecifier);
					return (char)value;
				}
				//** 0[01]?[0-7]{0,5}
				{
					int value = 0;
					foreach(char ch in tokenValue)
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

				//** n
				return '\n';

				//** r
				return '\r';

				//** t
				return '\t';

				//** v
				return '\v';

				//**
				return (char)Take();
			}
		}
	}
}
