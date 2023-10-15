namespace Pard {
	interface IGrammarOutput {
		void Write(IReadOnlyList<Grammar.ActionEntry> actions, IReadOnlyList<ActionCode> codeBlocks, IReadOnlyList<Grammar.GotoEntry> gotos, IReadOnlyList<Production> productions, TextWriter writer, Options options);
	}
}
