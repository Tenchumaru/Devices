namespace Pard {
	public interface IGrammarInput {
		IReadOnlyList<Production> Read(TextReader reader);
	}
}
