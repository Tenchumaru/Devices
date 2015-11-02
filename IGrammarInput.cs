using System.IO;

namespace Pard
{
	interface IGrammarInput
	{
		Grammar Read(TextReader reader);
	}
}
