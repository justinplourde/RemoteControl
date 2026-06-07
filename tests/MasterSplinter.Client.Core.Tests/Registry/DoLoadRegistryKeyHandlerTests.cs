using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Registry;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Registry
{
    [TestClass]
    public class DoLoadRegistryKeyHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsRegistryMatches()
        {
            var handler = new DoLoadRegistryKeyHandler(new TestRegistryKeyProvider(
                RegistryKeyLoadResult.Success(new[]
                {
                    new RegSeekerMatch
                    {
                        Key = "Software",
                        HasSubKeys = true,
                        Data = new[]
                        {
                            new RegValueData
                            {
                                Name = "",
                                Kind = (RegistryValueKind)1,
                                Data = new byte[0]
                            }
                        }
                    }
                })));

            var response = (GetRegistryKeysResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoLoadRegistryKey { RootKeyName = "HKCU" },
                CancellationToken.None);

            Assert.AreEqual("HKCU", response.RootKey);
            Assert.IsFalse(response.IsError);
            Assert.AreEqual(1, response.Matches.Length);
            Assert.AreEqual("Software", response.Matches[0].Key);
            Assert.IsTrue(response.Matches[0].HasSubKeys);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsRegistryError()
        {
            var handler = new DoLoadRegistryKeyHandler(new TestRegistryKeyProvider(
                RegistryKeyLoadResult.Error("Invalid rootkey, could not be found.")));

            var response = (GetRegistryKeysResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoLoadRegistryKey { RootKeyName = "NOPE" },
                CancellationToken.None);

            Assert.IsTrue(response.IsError);
            Assert.AreEqual("Invalid rootkey, could not be found.", response.ErrorMsg);
            Assert.AreEqual(0, response.Matches.Length);
        }

        private sealed class TestRegistryKeyProvider : IRegistryKeyProvider
        {
            private readonly RegistryKeyLoadResult _result;

            public TestRegistryKeyProvider(RegistryKeyLoadResult result)
            {
                _result = result;
            }

            public RegistryKeyLoadResult LoadKey(string rootKeyName)
            {
                return _result;
            }
        }

        private sealed class TestClientContext : IClientContext
        {
            public TestClientContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }
        }
    }
}
