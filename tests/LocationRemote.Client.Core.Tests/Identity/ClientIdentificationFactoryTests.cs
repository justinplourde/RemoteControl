using LocationRemote.Client.Core.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using Quasar.Common.Protocol;

namespace LocationRemote.Client.Core.Tests.Identity
{
    [TestClass]
    public class ClientIdentificationFactoryTests
    {
        [TestMethod, TestCategory("Identity")]
        public void CreateMapsIdentityOptionsToProtocolMessage()
        {
            var protocolVersion = new ProtocolVersion { Major = 1, Minor = 2 };
            var capabilities = new ClientCapabilities();
            capabilities.SupportedFeatures.Add("handshake");
            var signature = new byte[] { 1, 2, 3, 4 };
            var options = new ClientIdentityOptions(
                "modern-dev",
                "Windows 11",
                "Admin",
                "United States",
                "US",
                230,
                "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                "jplou",
                "LOCATIONREMOTE",
                "modern",
                "dev-key",
                signature,
                protocolVersion,
                capabilities);

            ClientIdentification identification = new ClientIdentificationFactory().Create(options);

            Assert.AreEqual(options.Version, identification.Version);
            Assert.AreEqual(options.OperatingSystem, identification.OperatingSystem);
            Assert.AreEqual(options.AccountType, identification.AccountType);
            Assert.AreEqual(options.Country, identification.Country);
            Assert.AreEqual(options.CountryCode, identification.CountryCode);
            Assert.AreEqual(options.ImageIndex, identification.ImageIndex);
            Assert.AreEqual(options.ClientId, identification.Id);
            Assert.AreEqual(options.Username, identification.Username);
            Assert.AreEqual(options.MachineName, identification.PcName);
            Assert.AreEqual(options.Tag, identification.Tag);
            Assert.AreEqual(options.EncryptionKey, identification.EncryptionKey);
            CollectionAssert.AreEqual(signature, identification.Signature);
            Assert.AreSame(protocolVersion, identification.ProtocolVersion);
            Assert.AreSame(capabilities, identification.Capabilities);
        }
    }
}
