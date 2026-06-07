using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Registry;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Registry
{
    [TestClass]
    public class RegistryKeyMutationHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task CreateHandlerReturnsCreateResponse()
        {
            var provider = new RecordingProvider(RegistryKeyMutationResult.Success(
                "New Key #1",
                new RegSeekerMatch { Key = "New Key #1", Data = new RegValueData[0], HasSubKeys = false }));
            var handler = new DoCreateRegistryKeyHandler(provider);

            var response = (GetCreateRegistryKeyResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoCreateRegistryKey { ParentPath = "HKCU\\Software" },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.CreateParentPath);
            Assert.AreEqual("HKCU\\Software", response.ParentPath);
            Assert.AreEqual("New Key #1", response.Match.Key);
            Assert.IsFalse(response.IsError);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task DeleteHandlerReturnsDeleteErrorResponse()
        {
            var provider = new RecordingProvider(RegistryKeyMutationResult.Error("Denied", "Old"));
            var handler = new DoDeleteRegistryKeyHandler(provider);

            var response = (GetDeleteRegistryKeyResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoDeleteRegistryKey { ParentPath = "HKCU\\Software", KeyName = "Old" },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.DeleteParentPath);
            Assert.AreEqual("Old", provider.DeleteKeyName);
            Assert.AreEqual("Old", response.KeyName);
            Assert.IsTrue(response.IsError);
            Assert.AreEqual("Denied", response.ErrorMsg);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task RenameHandlerReturnsRenameResponse()
        {
            var provider = new RecordingProvider(RegistryKeyMutationResult.Success("New", null));
            var handler = new DoRenameRegistryKeyHandler(provider);

            var response = (GetRenameRegistryKeyResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoRenameRegistryKey { ParentPath = "HKCU\\Software", OldKeyName = "Old", NewKeyName = "New" },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.RenameParentPath);
            Assert.AreEqual("Old", provider.RenameOldKeyName);
            Assert.AreEqual("New", provider.RenameNewKeyName);
            Assert.IsFalse(response.IsError);
        }

        private sealed class RecordingProvider : IRegistryKeyMutationProvider
        {
            private readonly RegistryKeyMutationResult _result;

            public RecordingProvider(RegistryKeyMutationResult result)
            {
                _result = result;
            }

            public string CreateParentPath { get; private set; }

            public string DeleteParentPath { get; private set; }

            public string DeleteKeyName { get; private set; }

            public string RenameParentPath { get; private set; }

            public string RenameOldKeyName { get; private set; }

            public string RenameNewKeyName { get; private set; }

            public RegistryKeyMutationResult CreateKey(string parentPath)
            {
                CreateParentPath = parentPath;
                return _result;
            }

            public RegistryKeyMutationResult DeleteKey(string parentPath, string keyName)
            {
                DeleteParentPath = parentPath;
                DeleteKeyName = keyName;
                return _result;
            }

            public RegistryKeyMutationResult RenameKey(string parentPath, string oldKeyName, string newKeyName)
            {
                RenameParentPath = parentPath;
                RenameOldKeyName = oldKeyName;
                RenameNewKeyName = newKeyName;
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
