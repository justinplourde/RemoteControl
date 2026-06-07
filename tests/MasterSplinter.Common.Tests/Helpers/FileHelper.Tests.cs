using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Helpers;

namespace MasterSplinter.Common.Tests.Helpers
{
    [TestClass]
    public class FileHelperTests
    {
        [TestMethod, TestCategory("Helpers")]
        public void RandomFilenameTest()
        {
            int length = 100;
            var name = FileHelper.GetRandomFilename(length);

            Assert.IsNotNull(name);
            Assert.IsTrue(name.Length == length);
        }

        [TestMethod, TestCategory("Helpers")]
        public void ValidateExecutableTest()
        {
            var bytes = new byte[] {77, 90};

            Assert.IsTrue(FileHelper.HasExecutableIdentifier(bytes));
        }

        [TestMethod, TestCategory("Helpers")]
        public void ValidateExecutableTest2()
        {
            var bytes = new byte[] {22, 93};

            Assert.IsFalse(FileHelper.HasExecutableIdentifier(bytes));
        }

        [TestMethod, TestCategory("Helpers")]
        public void RandomFilenameWithExtensionTest()
        {
            var name = FileHelper.GetRandomFilename(12, ".exe");

            Assert.IsTrue(name.EndsWith(".exe"));
            Assert.AreEqual(16, name.Length);
        }

        [TestMethod, TestCategory("Helpers")]
        public void ValidateShortExecutableHeaderTest()
        {
            var bytes = new byte[] {77};

            Assert.IsFalse(FileHelper.HasExecutableIdentifier(bytes));
        }
    }
}
