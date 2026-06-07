using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Services;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Services
{
    [TestClass]
    public class DoShutdownActionHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStatusForSuccessfulAction()
        {
            var provider = new TestShutdownActionProvider(ShutdownActionResult.Success());
            var handler = new DoShutdownActionHandler(provider);

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoShutdownAction { Action = ShutdownAction.Restart },
                CancellationToken.None);

            Assert.AreEqual(ShutdownAction.Restart, provider.Action);
            Assert.AreEqual("Restart requested.", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsLegacyStyleFailureStatus()
        {
            var provider = new TestShutdownActionProvider(ShutdownActionResult.Error("Denied"));
            var handler = new DoShutdownActionHandler(provider);

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoShutdownAction { Action = ShutdownAction.Shutdown },
                CancellationToken.None);

            Assert.AreEqual(ShutdownAction.Shutdown, provider.Action);
            Assert.AreEqual("Action failed: Denied", response.Message);
        }

        private sealed class TestShutdownActionProvider : IShutdownActionProvider
        {
            private readonly ShutdownActionResult _result;

            public TestShutdownActionProvider(ShutdownActionResult result)
            {
                _result = result;
            }

            public ShutdownAction Action { get; private set; }

            public ShutdownActionResult RequestAction(ShutdownAction action)
            {
                Action = action;
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
