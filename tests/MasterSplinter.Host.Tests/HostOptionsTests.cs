using ClientHostOptions = MasterSplinter.Client.Host.HostOptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServerHostOptions = MasterSplinter.Server.Host.HostOptions;
using System;

namespace MasterSplinter.Host.Tests
{
    [TestClass]
    public class HostOptionsTests
    {
        [TestMethod, TestCategory("Host")]
        public void ServerOptionsParseDefaultsAndFlags()
        {
            ServerHostOptions defaults = ServerHostOptions.Parse(Array.Empty<string>());
            ServerHostOptions custom = ServerHostOptions.Parse(new[]
            {
                "--host", "localhost",
                "--port", "47829",
                "--smoke-test",
                "--once",
                "--dispatch", "get-system-info"
            });

            Assert.AreEqual("127.0.0.1", defaults.Host);
            Assert.AreEqual(4782, defaults.Port);
            Assert.IsFalse(defaults.SmokeTest);
            Assert.IsFalse(defaults.Once);
            Assert.AreEqual("localhost", custom.Host);
            Assert.AreEqual(47829, custom.Port);
            Assert.IsTrue(custom.SmokeTest);
            Assert.IsTrue(custom.Once);
            Assert.AreEqual("get-system-info", custom.DispatchCommand);
        }

        [TestMethod, TestCategory("Host")]
        public void ClientOptionsParseDefaultsAndIdentityInputs()
        {
            ClientHostOptions custom = ClientHostOptions.Parse(new[]
            {
                "--host", "localhost",
                "--port", "47829",
                "--client-id", "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                "--tag", "lab",
                "--encryption-key", "test-key",
                "--smoke-test",
                "--handle-one-command"
            });

            Assert.AreEqual("localhost", custom.Host);
            Assert.AreEqual(47829, custom.Port);
            Assert.AreEqual("BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", custom.ClientId);
            Assert.AreEqual("lab", custom.Tag);
            Assert.AreEqual("test-key", custom.EncryptionKey);
            Assert.IsTrue(custom.SmokeTest);
            Assert.IsTrue(custom.HandleOneCommand);
        }

        [TestMethod, TestCategory("Host")]
        public void UnknownOptionsAreRejected()
        {
            Assert.ThrowsException<ArgumentException>(() => ServerHostOptions.Parse(new[] { "--nope" }));
            Assert.ThrowsException<ArgumentException>(() => ClientHostOptions.Parse(new[] { "--nope" }));
        }
    }
}
