using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Startup;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Startup
{
    [TestClass]
    public class StartupItemMutationHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task AddHandlerReturnsSuccessStatus()
        {
            var provider = new RecordingMutationProvider(StartupItemMutationResult.Success());
            var handler = new DoStartupItemAddHandler(provider);

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoStartupItemAdd
                {
                    StartupItem = new StartupItem
                    {
                        Name = "Agent",
                        Path = "C:\\Tools\\agent.exe",
                        Type = StartupType.CurrentUserRun
                    }
                },
                CancellationToken.None);

            Assert.AreEqual("Agent", provider.Added.Name);
            Assert.AreEqual("C:\\Tools\\agent.exe", provider.Added.Path);
            Assert.AreEqual(StartupType.CurrentUserRun, provider.Added.Type);
            Assert.AreEqual("Added Autostart Item", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task RemoveHandlerReturnsFailureStatus()
        {
            var provider = new RecordingMutationProvider(
                StartupItemMutationResult.Error("Removing Autostart Item failed: Denied"));
            var handler = new DoStartupItemRemoveHandler(provider);

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoStartupItemRemove
                {
                    StartupItem = new StartupItem
                    {
                        Name = "Agent",
                        Type = StartupType.CurrentUserRun
                    }
                },
                CancellationToken.None);

            Assert.AreEqual("Agent", provider.Removed.Name);
            Assert.AreEqual(StartupType.CurrentUserRun, provider.Removed.Type);
            Assert.AreEqual("Removing Autostart Item failed: Denied", response.Message);
        }

        private sealed class RecordingMutationProvider : IStartupItemMutationProvider
        {
            private readonly StartupItemMutationResult _result;

            public RecordingMutationProvider(StartupItemMutationResult result)
            {
                _result = result;
            }

            public StartupItem Added { get; private set; }

            public StartupItem Removed { get; private set; }

            public StartupItemMutationResult AddStartupItem(StartupItem startupItem)
            {
                Added = startupItem;
                return _result;
            }

            public StartupItemMutationResult RemoveStartupItem(StartupItem startupItem)
            {
                Removed = startupItem;
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
