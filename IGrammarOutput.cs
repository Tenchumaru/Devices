using System.Collections.Generic;
using System.IO;

namespace Pard
{
	interface IGrammarOutput
	{
		void Write(IEnumerable<Grammar.Entry> table, TextWriter writer, Options options);
	}
}
