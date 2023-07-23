using System.Text;
using static Pard.YaccInput;

namespace Pard {
	public partial class YaccInputScanner {
		internal static readonly Dictionary<char, char> knownEscapes = new() {
			{'a', '\a' },
			{'b', '\b' },
			{'f', '\f' },
			{'n', '\n' },
			{'r', '\r' },
			{'t', '\t' },
			{'v', '\v' },
		};
		private readonly TextReader reader;
		private int scopeLevel;
		private readonly StringBuilder codeBlock = new();
		private bool isCollecting;
		private Func<Token?> previousFn;
		private Func<Token?> fn;

		internal YaccInputScanner(TextReader reader) {
			this.reader = reader;
			previousFn = fn = ReadSectionOne;
		}

		internal Token? Read() => fn();

		private Token? ReadSectionThree() {
			fn = EndParse;
			string code = reader_.Consume(int.MaxValue);
			code += reader.ReadToEnd();
			return new Token { Symbol = YaccInputParser.CodeBlock, Value = new ActionCode(codeBlock.ToString(), LineNumber) };
		}

		private Token? EndParse() {
			return null;
		}

		private static void ReportError(string message) => Console.Error.WriteLine(message);

		private static char Unescape(string value) {
			if (value.Length == 1) {
				return value[0];
			} else if (value.Length == 2) {
				if (value[0] == '\\') {
					if (knownEscapes.TryGetValue(value[1], out char c)) {
						return c;
					}
					return value[1];
				}
			}
			throw new NotImplementedException();
		}
	}
}
