using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Listeners
{
    [TestClass]
    public class ClientStatusMessageSinkTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public async Task SetStatusUpdatesRegistryAndForwardsMessage()
        {
            var registry = new ClientStatusRegistry();
            var innerSink = new RecordingMessageSink();
            var sink = new ClientStatusMessageSink(registry, innerSink);
            var connection = new TestRemoteClientConnection("connection-1", "client-1");
            var message = new SetStatus { Message = "Ready" };

            await sink.HandleAsync(connection, message, CancellationToken.None);

            Assert.IsTrue(registry.TryGet("client-1", out ClientStatusSnapshot snapshot));
            Assert.AreEqual("Ready", snapshot.StatusMessage);
            Assert.AreSame(message, innerSink.Messages[0]);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task SetUserStatusUpdatesRegistryWithoutForwardingMessage()
        {
            var registry = new ClientStatusRegistry();
            var innerSink = new RecordingMessageSink();
            var sink = new ClientStatusMessageSink(registry, innerSink);
            var connection = new TestRemoteClientConnection("connection-1", "client-1");

            await sink.HandleAsync(
                connection,
                new SetUserStatus { Message = UserStatus.Active },
                CancellationToken.None);

            Assert.IsTrue(registry.TryGet("client-1", out ClientStatusSnapshot snapshot));
            Assert.AreEqual(UserStatus.Active, snapshot.UserStatus);
            Assert.AreEqual(0, innerSink.Messages.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task NonStatusMessageIsForwarded()
        {
            var registry = new ClientStatusRegistry();
            var innerSink = new RecordingMessageSink();
            var sink = new ClientStatusMessageSink(registry, innerSink);
            var connection = new TestRemoteClientConnection("connection-1", "client-1");
            var message = new GetSystemInfo();

            await sink.HandleAsync(connection, message, CancellationToken.None);

            Assert.AreSame(message, innerSink.Messages[0]);
            Assert.IsFalse(registry.TryGet("client-1", out _));
        }

        private sealed class RecordingMessageSink : IRemoteClientMessageSink
        {
            public List<IMessage> Messages { get; } = new List<IMessage>();

            public Task HandleAsync(
                IRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                Messages.Add(message);
                return Task.CompletedTask;
            }
        }

        private sealed class TestRemoteClientConnection : IRemoteClientConnection
        {
            public TestRemoteClientConnection(string connectionId, string clientId)
            {
                ConnectionId = connectionId;
                ClientId = clientId;
                ConnectedAtUtc = DateTimeOffset.UtcNow;
                LastSeenUtc = ConnectedAtUtc;
            }

            public string ConnectionId { get; }

            public string ClientId { get; }

            public ClientIdentification Identification => new ClientIdentification { Id = ClientId };

            public bool IsIdentified => true;

            public bool IsConnected => true;

            public DateTimeOffset ConnectedAtUtc { get; }

            public DateTimeOffset LastSeenUtc { get; }

            public void SetIdentification(ClientIdentification identification)
            {
            }

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task DisconnectAsync(string reason, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
