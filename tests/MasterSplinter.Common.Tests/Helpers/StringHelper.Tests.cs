using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Helpers;

namespace MasterSplinter.Common.Tests.Helpers
{
    [TestClass]
    public class StringHelperTests
    {
        [TestMethod, TestCategory("Helpers")]
        public void HumanReadableFileSizeTest()
        {
            Assert.AreEqual("1 KB", StringHelper.GetHumanReadableFileSize(1024));
            Assert.AreEqual("1.5 KB", StringHelper.GetHumanReadableFileSize(1536));
        }

        [TestMethod, TestCategory("Helpers")]
        public void FormatMacAddressTest()
        {
            Assert.AreEqual("AA:BB:CC:DD:EE:FF", StringHelper.GetFormattedMacAddress("AABBCCDDEEFF"));
            Assert.AreEqual("00:00:00:00:00:00", StringHelper.GetFormattedMacAddress("bad"));
        }

        [TestMethod, TestCategory("Helpers")]
        public void RemoveLastCharsTest()
        {
            Assert.AreEqual("Loc", StringHelper.RemoveLastChars("Location", 5));
            Assert.AreEqual("Loc", StringHelper.RemoveLastChars("Loc", 5));
        }
    }
}
