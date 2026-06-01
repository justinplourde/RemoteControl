using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Cryptography;
using Quasar.Common.Helpers;
using System.Text;

namespace Quasar.Common.Tests.Cryptography
{
    [TestClass]
    public class Sha256Tests
    {
        [TestMethod, TestCategory("Cryptography")]
        public void ComputeHashTest()
        {
            var input = StringHelper.GetRandomString(100);
            var result = Sha256.ComputeHash(input);

            Assert.IsNotNull(result);
            Assert.AreNotEqual(result, input);
        }

        [TestMethod, TestCategory("Cryptography")]
        public void ComputeHashByteArrayTest()
        {
            var input = Encoding.UTF8.GetBytes("MasterSplinter");
            var result = Sha256.ComputeHash(input);

            Assert.AreEqual(32, result.Length);
        }
    }
}
