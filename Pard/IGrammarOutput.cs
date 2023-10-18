namespace Pard {
	interface IGrammarOutput {
		void Write(IEnumerable<(string, int)> namedTerminals, IReadOnlyList<Grammar.ActionEntry> actions, IReadOnlyList<ActionCode> codeBlocks, IReadOnlyList<Grammar.GotoEntry> gotos, IReadOnlyList<Production> productions, TextWriter writer, Options options);
	}
}
