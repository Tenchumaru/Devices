namespace Pard {
	public interface IGrammarInput {
		(Nonterminal, IReadOnlyList<Production>) Read(TextReader reader);
	}
}
