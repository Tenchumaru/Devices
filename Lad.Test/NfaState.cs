namespace Lad.Test {
  [TestClass]
  public class NfaStateTest {
    [TestMethod]
    public void Doit() {
      NfaState nfaState = new();
      Assert.AreEqual(1, nfaState.Number);
    }
  }
}
