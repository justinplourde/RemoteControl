using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Registry;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Registry
{
#pragma warning disable CA1416
    [TestClass]
    public class RegistryValueMutationHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task CreateHandlerReturnsCreateValueResponse()
        {
            var value = new RegValueData { Name = "New Value #1", Kind = RegistryValueKind.String, Data = new byte[0] };
            var provider = new RecordingProvider(RegistryValueMutationResult.Success("New Value #1", value));
            var handler = new DoCreateRegistryValueHandler(provider);

            var response = (GetCreateRegistryValueResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoCreateRegistryValue { KeyPath = "HKCU\\Software", Kind = RegistryValueKind.String },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.CreateKeyPath);
            Assert.AreEqual(RegistryValueKind.String, provider.CreateKind);
            Assert.AreEqual("HKCU\\Software", response.KeyPath);
            Assert.AreEqual("New Value #1", response.Value.Name);
            Assert.IsFalse(response.IsError);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task DeleteHandlerReturnsDeleteValueErrorResponse()
        {
            var provider = new RecordingProvider(RegistryValueMutationResult.Error("Denied", "Old"));
            var handler = new DoDeleteRegistryValueHandler(provider);

            var response = (GetDeleteRegistryValueResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoDeleteRegistryValue { KeyPath = "HKCU\\Software", ValueName = "Old" },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.DeleteKeyPath);
            Assert.AreEqual("Old", provider.DeleteValueName);
            Assert.AreEqual("Old", response.ValueName);
            Assert.IsTrue(response.IsError);
            Assert.AreEqual("Denied", response.ErrorMsg);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task RenameHandlerReturnsRenameValueResponse()
        {
            var provider = new RecordingProvider(RegistryValueMutationResult.Success("New", null));
            var handler = new DoRenameRegistryValueHandler(provider);

            var response = (GetRenameRegistryValueResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoRenameRegistryValue { KeyPath = "HKCU\\Software", OldValueName = "Old", NewValueName = "New" },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.RenameKeyPath);
            Assert.AreEqual("Old", provider.RenameOldValueName);
            Assert.AreEqual("New", provider.RenameNewValueName);
            Assert.IsFalse(response.IsError);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ChangeHandlerReturnsChangeValueResponse()
        {
            var value = new RegValueData { Name = "Name", Kind = RegistryValueKind.DWord, Data = new byte[] { 42, 0, 0, 0 } };
            var provider = new RecordingProvider(RegistryValueMutationResult.Success("Name", value));
            var handler = new DoChangeRegistryValueHandler(provider);

            var response = (GetChangeRegistryValueResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoChangeRegistryValue { KeyPath = "HKCU\\Software", Value = value },
                CancellationToken.None);

            Assert.AreEqual("HKCU\\Software", provider.ChangeKeyPath);
            Assert.AreSame(value, provider.RecordedChangeValue);
            Assert.AreSame(value, response.Value);
            Assert.IsFalse(response.IsError);
        }

        private sealed class RecordingProvider : IRegistryValueMutationProvider
        {
            private readonly RegistryValueMutationResult _result;

            public RecordingProvider(RegistryValueMutationResult result)
            {
                _result = result;
            }

            public string CreateKeyPath { get; private set; }

            public RegistryValueKind CreateKind { get; private set; }

            public string DeleteKeyPath { get; private set; }

            public string DeleteValueName { get; private set; }

            public string RenameKeyPath { get; private set; }

            public string RenameOldValueName { get; private set; }

            public string RenameNewValueName { get; private set; }

            public string ChangeKeyPath { get; private set; }

            public RegValueData RecordedChangeValue { get; private set; }

            public RegistryValueMutationResult CreateValue(string keyPath, RegistryValueKind kind)
            {
                CreateKeyPath = keyPath;
                CreateKind = kind;
                return _result;
            }

            public RegistryValueMutationResult DeleteValue(string keyPath, string valueName)
            {
                DeleteKeyPath = keyPath;
                DeleteValueName = valueName;
                return _result;
            }

            public RegistryValueMutationResult RenameValue(string keyPath, string oldValueName, string newValueName)
            {
                RenameKeyPath = keyPath;
                RenameOldValueName = oldValueName;
                RenameNewValueName = newValueName;
                return _result;
            }

            public RegistryValueMutationResult ChangeValue(string keyPath, RegValueData value)
            {
                ChangeKeyPath = keyPath;
                RecordedChangeValue = value;
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
#pragma warning restore CA1416
}
