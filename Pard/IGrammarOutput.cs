using System.Collections.Generic;
using System.IO;

namespace Pard
{
    interface IGrammarOutput
    {
        void Write(IReadOnlyList<Grammar.ActionEntry> actions, IReadOnlyList<Grammar.GotoEntry> gotos, IReadOnlyList<Production> productions, TextWriter writer, Options options);
    }
}
