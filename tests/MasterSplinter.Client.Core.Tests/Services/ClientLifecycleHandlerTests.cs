using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Services;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Services
{
    [TestClass]
    public class ClientLifecycleHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task DisconnectHandlerSendsStatusAndRequestsDisconnect()
        {
            var context = new RecordingLifecycleContext("client-1");
            var handler = new DoClientDisconnectHandler();

            await handler.HandleAsync(context, new DoClientDisconnect(), CancellationToken.None);

            Assert.IsTrue(context.DisconnectRequested);
            Assert.IsFalse(context.ReconnectRequested);
            Assert.AreEqual("Client disconnect requested.", ((SetStatus)context.Sent[0]).Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ReconnectHandlerSendsStatusAndRequestsReconnect()
        {
            var context = new RecordingLifecycleContext("client-1");
            var handler = new DoClientReconnectHandler();

            await handler.HandleAsync(context, new DoClientReconnect(), CancellationToken.None);

            Assert.IsFalse(context.DisconnectRequested);
            Assert.IsTrue(context.ReconnectRequested);
            Assert.AreEqual("Client reconnect requested.", ((SetStatus)context.Sent[0]).Message);
        }

        private sealed class RecordingLifecycleContext : IClientLifecycleContext
        {
            public RecordingLifecycleContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public bool DisconnectRequested { get; private set; }

            public bool ReconnectRequested { get; private set; }

            public List<IMessage> Sent { get; } = new List<IMessage>();

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }

            public Task RequestDisconnectAsync(CancellationToken cancellationToken)
            {
                DisconnectRequested = true;
                return Task.CompletedTask;
            }

            public Task RequestReconnectAsync(CancellationToken cancellationToken)
            {
                ReconnectRequested = true;
                return Task.CompletedTask;
            }
        }
    }
}
