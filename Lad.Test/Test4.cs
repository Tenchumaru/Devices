namespace Lad.Test {
	[TestClass]
	public partial class Test4 {
		// Test a single carriage return accepted as "any" when "any" means "any character except newline" and newline is either a
		// POSIX newline or a Windows newline.  This specifically tests handling a carriage return at EOF.
		private readonly TextReader reader = new StringReader("\r");

		[TestMethod]
		public void Doit() {
			// Assemble
			var expected = new[] { "\r" };

			// Act
			string[] actual = Enumerable.Range(0, int.MaxValue).Select(_ => Read()).TakeWhile(t => t != null).Select(t => t!.Value).ToArray();

			// Assert
			Assert.AreEqual(expected.Length, actual.Length);
			var q = expected.Zip(actual);
			foreach ((string First, string Second) in q) {
				Assert.AreEqual(First, Second);
			}
		}

		internal class Token {
			internal string Value;

			internal Token(string value) {
				Value = value;
			}
		}
	}
}
