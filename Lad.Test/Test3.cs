namespace Lad.Test {
	[TestClass]
	public partial class Test3 {
		private readonly TextReader reader = new StringReader("aa ab abbabbaba abc zx zxx zxxy zxy zz");

		[TestMethod]
		public void Doit() {
			// Assemble
			var expected = new[] { "ab", "ab", "a", "z", "zx", "zx", "z" };

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
