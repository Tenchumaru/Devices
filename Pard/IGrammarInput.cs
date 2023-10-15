namespace Pard {
	public interface IGrammarInput {
		(Nonterminal, IReadOnlyList<Production>, IReadOnlyList<ActionCode>) Read(TextReader reader);
	}
}
