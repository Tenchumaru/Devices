using System.IO;

namespace Pard
{
	interface IGrammarInput
	{
		bool Read(TextReader reader, Grammar grammar);
	}
}
