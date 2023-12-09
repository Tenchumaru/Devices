using static Pard.Grammar;
using static Pard.YaccInput;

#pragma warning disable CS0414 // The field 'YaccInputScanner.*' is assigned but its value is never used
#pragma warning disable IDE0051 // Remove unused private members

namespace Pard {
	public partial class YaccInputScanner {
		private const string ws = "[\a\b\f\n\r\t\v ]+";
		private const string ch = "'([^\\']|\\.)+'";
		private const string str = "\"([^\\\"]|\\.)*\"";
		private const string hex = "[0-9A-Fa-f]";
		private const string id = "[A-Za-z_][0-9A-Za-z_]*";

		private Token? ReadSectionOne(string? pattern, string tokenValue) {
			switch (pattern) {
				case "%define":
					return new Token { Symbol = YaccInputParser.PDefine };
				case "%start":
					return new Token { Symbol = YaccInputParser.PStart };
				case "%left":
					return new Token { Symbol = YaccInputParser.PToken, Value = new KeyValuePair<Associativity, int?>(Associativity.Left, ++precedence) };
				case "%right":
					return new Token { Symbol = YaccInputParser.PToken, Value = new KeyValuePair<Associativity, int?>(Associativity.Right, ++precedence) };
				case "%nonassoc":
					return new Token { Symbol = YaccInputParser.PToken, Value = new KeyValuePair<Associativity, int?>(Associativity.Nonassociative, ++precedence) };
				case "%token":
					return new Token { Symbol = YaccInputParser.PToken, Value = new KeyValuePair<Associativity, int?>(Associativity.None, null) };
				case "%precedence":
					return new Token { Symbol = YaccInputParser.PToken, Value = new KeyValuePair<Associativity, int?>(Associativity.None, ++precedence) };
				case "%type":
					return new Token { Symbol = YaccInputParser.PType };
				case "%{id}":
					IgnoreUnknown();
					return new Token { Symbol = YaccInputParser.PUnknown, Value = tokenValue[1..] };
				case "{id}":
					return new Token { Symbol = YaccInputParser.Identifier, Value = tokenValue };
				case "%%":
					fn = ReadSectionTwo;
					return new Token { Symbol = YaccInputParser.PP };
				case @"%\{":
					previousFn = fn;
					fn = ReadCodeBlock;
					codeBlock.Clear();
					codeBlockLineNumber = LineNumber;
					scopeLevel = 1;
					return new Token { Symbol = YaccInputParser.POCB };
				case @"%\}":
					return new Token { Symbol = YaccInputParser.PCCB };
				case "{ch}":
					return new Token { Symbol = YaccInputParser.Literal, Value = Unescape(tokenValue[1..^1]) };
				case "<|>":
					return new Token { Symbol = tokenValue[0] };
				case "$/*":
					// Ignore comments.
					isCollecting = false;
					if (!CollectComments().HasValue) {
						ReportError("unexpected end of input");
					}
					break;
				case @"\/\/.*":
					// Ignore remarks.
					break;
				case "{ws}":
					// Ignore white space.
					break;
				default:
					if (tokenValue.Any()) {
						ReportError($"unexpected input in line {LineNumber}: '{tokenValue}'");
					} else {
						ReportError("unexpected end of input");
					}
					return Token.End;
			}
			return null;
		}

		private Token? IgnoreUnknown(string? pattern, string tokenValue) {
			switch (pattern) {
				case ".+":
					// Ignore all characters.
					break;
				case @"\N":
					// The ReadSectionOne method invokes this one and ignores its return value.
					return new Token();
			}
			return null;
		}

		private Token? ReadSectionTwo(string? pattern, string tokenValue) {
			switch (pattern) {
				case "%%":
					fn = ReadSectionThree;
					return new Token { Symbol = YaccInputParser.PP };
				case "%empty":
					// Ignore empty directives.
					break;
				case "%prec":
					return new Token { Symbol = YaccInputParser.PPrec };
				case "error":
					return new Token { Symbol = YaccInputParser.ErrorToken };
				case "{id}":
					return new Token { Symbol = YaccInputParser.Identifier, Value = tokenValue };
				case "{ch}":
					return new Token { Symbol = YaccInputParser.Literal, Value = Unescape(tokenValue[1..^1]) };
				case "${":
					previousFn = fn;
					codeBlock.Clear();
					codeBlock.Append('{');
					codeBlockLineNumber = LineNumber;
					scopeLevel = 1;
					return ReadCodeBlock();
				case @"\/\/.*":
					// Ignore remarks.
					break;
				case "$/*":
					// Ignore comments.
					isCollecting = false;
					if (!CollectComments().HasValue) {
						ReportError("unexpected end of input");
					}
					break;
				case "{ws}":
					// Ignore white space.
					break;
				default:
					return new Token { Symbol = tokenValue[0] };
			}
			return null;
		}

		private Token? ReadCodeBlock(string? pattern, string tokenValue) {
			switch (pattern) {
				case "{str}":
					codeBlock.Append(tokenValue);
					break;
				case "{ch}":
					codeBlock.Append(tokenValue);
					break;
				case @"\/\/.*":
					codeBlock.Append(tokenValue);
					break;
				case "$/*":
					codeBlock.Append(tokenValue);
					isCollecting = true;
					if (!CollectComments().HasValue) {
						ReportError("unexpected end of input");
					}
					break;
				case "${":
					codeBlock.Append('{');
					++scopeLevel;
					break;
				case "$}":
					codeBlock.Append('}');
					if (--scopeLevel == 0) {
						if (previousFn == ReadSectionOne) {
							if (codeBlock[^2] != '%') {
								ReportError($"unmatched action braces in section one at line {LineNumber}");
								return Token.End;
							}
							codeBlock.Length -= 2;
							reader_!.Write("%}");
						}
						fn = previousFn;
						return new Token { Symbol = YaccInputParser.CodeBlock, Value = new ActionCode(codeBlock.ToString(), codeBlockLineNumber) };
					}
					break;
				case "[^\"'/{}]+":
				default:
					codeBlock.Append(tokenValue);
					break;
			}
			return null;
		}

		private bool? CollectComments(string? pattern, string tokenValue) {
			switch (pattern) {
				case "$*/":
					if (isCollecting) {
						codeBlock.Append(tokenValue);
					}
					return true;
				case "[^*]+":
				default:
					if (isCollecting) {
						codeBlock.Append(tokenValue);
					}
					break;
			}
			return null;
		}
	}
}
