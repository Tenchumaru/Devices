using System.Collections.Generic;
using System.IO;

namespace Pard {
	interface IGrammarInput {
		IReadOnlyList<Production> Read(TextReader reader, Options options);
	}
}
