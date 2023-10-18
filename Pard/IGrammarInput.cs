namespace Pard {
	public interface IGrammarInput {
		(Nonterminal, IEnumerable<(string, int)>, IReadOnlyList<Production>, IReadOnlyList<ActionCode>) Read(TextReader reader);
	}
}
