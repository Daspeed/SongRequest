using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongRequest.Utils;

namespace SongRequest.Test
{
    /// <summary>
    /// </summary>
    [TestClass]
    public class StringTest
    {
        [TestMethod]
        public void TestContains()
        {
            Assert.IsFalse("ë".Contains("e"), "ë");

            Assert.IsFalse("ë".ContainsOrdinalIgnoreCase("e"), "ë");
            Assert.IsTrue("E".ContainsOrdinalIgnoreCase("e"), "E");

            Assert.IsTrue("ë".ContainsIgnoreCaseNonSpace("e"), "ë");
            Assert.IsTrue("ø".ContainsIgnoreCaseNonSpace("o"), "ø");
            Assert.IsFalse("ø".ContainsIgnoreCaseNonSpace("e"), "ø");
            Assert.IsTrue("E".ContainsIgnoreCaseNonSpace("e"), "E");
            Assert.IsTrue("$".ContainsIgnoreCaseNonSpace("s"), "$");
            Assert.IsTrue("$".ContainsIgnoreCaseNonSpace("S"), "$");
            Assert.IsTrue("¹".ContainsIgnoreCaseNonSpace("1"), "¹");
            Assert.IsTrue("²".ContainsIgnoreCaseNonSpace("2"), "²");
            Assert.IsTrue("³".ContainsIgnoreCaseNonSpace("3"), "³");
        }
    }
}
