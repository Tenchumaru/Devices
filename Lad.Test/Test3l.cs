namespace Lad.Test {
	[TestClass]
	public partial class Test3l {
		private readonly TextReader reader = new StringReader("abbabbaba zxy zxyy zy zyy");

		[TestMethod]
		public void Doit() {
			// Assemble
			var expected = new[] { "ab", "ab", "zx", "zx", "z", "z" };

			// Act
			string[] actual = Enumerable.Range(0, int.MaxValue).Select(_ => Read()).TakeWhile(t => t != null).Select(t => t!.Value).ToArray();

			// Assert
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
