using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SongRequest;

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
            Assert.IsFalse("ë".Contains("e"));
            
            Assert.IsFalse("ë".ContainsOrdinalIgnoreCase("e"));
            Assert.IsTrue("E".ContainsOrdinalIgnoreCase("e"));

            Assert.IsTrue("ë".ContainsIgnoreCaseNonSpace("e"));
            Assert.IsTrue("ø".ContainsIgnoreCaseNonSpace("o"));
            Assert.IsFalse("ø".ContainsIgnoreCaseNonSpace("e"));
            Assert.IsTrue("E".ContainsIgnoreCaseNonSpace("e"));
        }
    }
}
