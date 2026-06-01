using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Startup;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Startup
{
    [TestClass]
    public class GetStartupItemsHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStartupItemsResponseFromProvider()
        {
            var items = new List<StartupItem>
            {
                new StartupItem
                {
                    Name = "Agent",
                    Path = "C:\\Tools\\agent.exe",
                    Type = StartupType.CurrentUserRun
                }
            };
            var handler = new GetStartupItemsHandler(new TestStartupItemProvider(
                StartupItemsResult.Success(items)));

            var response = (GetStartupItemsResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetStartupItems(),
                CancellationToken.None);

            Assert.AreEqual(1, response.StartupItems.Count);
            Assert.AreEqual("Agent", response.StartupItems[0].Name);
            Assert.AreEqual("C:\\Tools\\agent.exe", response.StartupItems[0].Path);
            Assert.AreEqual(StartupType.CurrentUserRun, response.StartupItems[0].Type);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStatusMessageForProviderError()
        {
            var handler = new GetStartupItemsHandler(new TestStartupItemProvider(
                StartupItemsResult.Error("Getting Autostart Items failed: Access denied")));

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetStartupItems(),
                CancellationToken.None);

            Assert.AreEqual("Getting Autostart Items failed: Access denied", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterSendsStartupItemsResponseThroughCommandContext()
        {
            var adapter = new ResponseMessageHandlerAdapter<GetStartupItems>(
                new GetStartupItemsHandler(new TestStartupItemProvider(
                    StartupItemsResult.Success(new List<StartupItem>
                    {
                        new StartupItem { Name = "Shortcut.url", Path = "C:\\Tools\\agent.exe", Type = StartupType.StartMenu }
                    }))));
            var context = new RecordingCommandContext("client-1");

            await adapter.HandleAsync(context, new GetStartupItems(), CancellationToken.None);

            Assert.IsInstanceOfType(context.SentMessage, typeof(GetStartupItemsResponse));
        }

        private sealed class TestStartupItemProvider : IStartupItemProvider
        {
            private readonly StartupItemsResult _result;

            public TestStartupItemProvider(StartupItemsResult result)
            {
                _result = result;
            }

            public StartupItemsResult GetStartupItems()
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

        private sealed class RecordingCommandContext : IClientCommandContext
        {
            public RecordingCommandContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public IMessage SentMessage { get; private set; }

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                SentMessage = message;
                return Task.CompletedTask;
            }
        }
    }
}
