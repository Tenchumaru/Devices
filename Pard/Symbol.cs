namespace Pard {
	public abstract class Symbol : NamedObject {
		public readonly string? TypeName;

		public Symbol(string name, string? typeName) : base(name) {
			TypeName = typeName;
		}
	}
}
