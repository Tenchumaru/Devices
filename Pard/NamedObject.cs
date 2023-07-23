namespace Pard {
	public abstract class NamedObject {
		public string Name { get; private set; }

		public NamedObject(string name) => Name = name;

		public override bool Equals(object? obj) => obj is NamedObject that && GetType() == that.GetType() && Name == that.Name;

		public override int GetHashCode() => Name.GetHashCode();

		public override string ToString() => Name;

		public static bool operator ==(NamedObject? left, NamedObject? right) => left is null ? right is null : left.Equals(right);

		public static bool operator !=(NamedObject? left, NamedObject? right) => !(left == right);
	}
}
