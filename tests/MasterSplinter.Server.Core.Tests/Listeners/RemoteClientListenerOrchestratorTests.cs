using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Listeners
{
    [TestClass]
    public class RemoteClientListenerOrchestratorTests
    {
        private const string ValidClientId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        [TestMethod, TestCategory("ServerCore")]
        public async Task StartAndStopDelegateToListener()
        {
            var listener = new RecordingListener();
            var orchestrator = CreateOrchestrator(listener);
            var options = new ServerListenOptions("127.0.0.1", 4782);

            await orchestrator.StartAsync(options, CancellationToken.None);
            await orchestrator.StopAsync(CancellationToken.None);

            Assert.AreSame(options, listener.Options);
            Assert.AreSame(orchestrator, listener.Handler);
            Assert.IsFalse(listener.IsListening);
            Assert.IsTrue(listener.StopCalled);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task ConnectedConnectionWritesLifecycleEvent()
        {
            var lifecycleSink = new RecordingLifecycleSink();
            var orchestrator = CreateOrchestrator(lifecycleSink);
            var connection = new TestRemoteClientConnection("connection-1");

            await orchestrator.ClientConnectedAsync(connection, CancellationToken.None);

            Assert.AreEqual(ClientConnectionLifecycleEventKind.Connected, lifecycleSink.Events[0].Kind);
            Assert.AreEqual("connection-1", lifecycleSink.Events[0].ConnectionId);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task IdentificationMessageRunsHandshakeAndSendsResult()
        {
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new RecordingLifecycleSink();
            var orchestrator = CreateOrchestrator(registry, lifecycleSink);
            var connection = new TestRemoteClientConnection("connection-1");

            await orchestrator.MessageReceivedAsync(
                connection,
                CreateIdentification(ValidClientId),
                CancellationToken.None);

            Assert.IsTrue(connection.IsIdentified);
            Assert.IsTrue(registry.TryGet(ValidClientId, out _));
            Assert.IsInstanceOfType(connection.SentMessages[0], typeof(ClientIdentificationResult));
            Assert.IsTrue(((ClientIdentificationResult)connection.SentMessages[0]).Result);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Identified, lifecycleSink.Events[0].Kind);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task RejectedIdentificationSendsFailureResultAndDisconnects()
        {
            var orchestrator = CreateOrchestrator(new ClientSessionRegistry(), new RecordingLifecycleSink());
            var connection = new TestRemoteClientConnection("connection-1");

            await orchestrator.MessageReceivedAsync(
                connection,
                CreateIdentification("too-short"),
                CancellationToken.None);

            Assert.IsFalse(((ClientIdentificationResult)connection.SentMessages[0]).Result);
            Assert.IsTrue(connection.DisconnectCalled);
            Assert.AreEqual("Client id must be 64 characters.", connection.DisconnectReason);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task UnidentifiedNonHandshakeMessageDisconnectsConnection()
        {
            var orchestrator = CreateOrchestrator();
            var connection = new TestRemoteClientConnection("connection-1");

            await orchestrator.MessageReceivedAsync(connection, new GetSystemInfo(), CancellationToken.None);

            Assert.IsTrue(connection.DisconnectCalled);
            Assert.AreEqual("Client must identify before sending messages.", connection.DisconnectReason);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task IdentifiedMessagesAreForwardedToMessageSink()
        {
            var messageSink = new RecordingMessageSink();
            var orchestrator = CreateOrchestrator(messageSink);
            var connection = new TestRemoteClientConnection("connection-1");
            connection.SetIdentification(CreateIdentification(ValidClientId));
            var message = new GetSystemInfo();

            await orchestrator.MessageReceivedAsync(connection, message, CancellationToken.None);

            Assert.AreSame(connection, messageSink.Connection);
            Assert.AreSame(message, messageSink.Message);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task DisconnectedAndFaultedConnectionsFlowToLifecycle()
        {
            var lifecycleSink = new RecordingLifecycleSink();
            var orchestrator = CreateOrchestrator(lifecycleSink);
            var connection = new TestRemoteClientConnection("connection-1");
            connection.SetIdentification(CreateIdentification(ValidClientId));
            var exception = new InvalidOperationException("boom");

            await orchestrator.ClientDisconnectedAsync(connection, "closed", CancellationToken.None);
            await orchestrator.ClientFaultedAsync(connection, exception, CancellationToken.None);

            Assert.AreEqual(ClientConnectionLifecycleEventKind.Disconnected, lifecycleSink.Events[0].Kind);
            Assert.AreEqual("closed", lifecycleSink.Events[0].Reason);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Faulted, lifecycleSink.Events[1].Kind);
            Assert.AreSame(exception, lifecycleSink.Events[1].Exception);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void ListenOptionsRejectInvalidValues()
        {
            Assert.ThrowsException<ArgumentException>(() => new ServerListenOptions("", 4782));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ServerListenOptions("127.0.0.1", 0));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ServerListenOptions("127.0.0.1", 65536));
        }

        private static RemoteClientListenerOrchestrator CreateOrchestrator()
        {
            return CreateOrchestrator(new ClientSessionRegistry(), new RecordingLifecycleSink());
        }

        private static RemoteClientListenerOrchestrator CreateOrchestrator(IRemoteClientMessageSink messageSink)
        {
            var registry = new ClientSessionRegistry();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            return new RemoteClientListenerOrchestrator(new RecordingListener(), lifecycle, handshake, messageSink);
        }

        private static RemoteClientListenerOrchestrator CreateOrchestrator(RecordingListener listener)
        {
            var registry = new ClientSessionRegistry();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            return new RemoteClientListenerOrchestrator(listener, lifecycle, handshake);
        }

        private static RemoteClientListenerOrchestrator CreateOrchestrator(RecordingLifecycleSink lifecycleSink)
        {
            return CreateOrchestrator(new ClientSessionRegistry(), lifecycleSink);
        }

        private static RemoteClientListenerOrchestrator CreateOrchestrator(
            IClientSessionRegistry registry,
            RecordingLifecycleSink lifecycleSink)
        {
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            return new RemoteClientListenerOrchestrator(new RecordingListener(), lifecycle, handshake);
        }

        private static ClientIdentification CreateIdentification(string clientId)
        {
            return new ClientIdentification
            {
                Id = clientId,
                Version = "1.4.1",
                OperatingSystem = "Windows 11 64 Bit",
                AccountType = "Admin",
                Username = "test-user",
                PcName = "test-machine",
                EncryptionKey = "test-key",
                Signature = new byte[] { 1, 2, 3, 4 }
            };
        }

        private sealed class RecordingListener : IRemoteClientListener
        {
            public bool IsListening { get; private set; }

            public bool StopCalled { get; private set; }

            public ServerListenOptions Options { get; private set; }

            public IRemoteClientListenerHandler Handler { get; private set; }

            public Task StartAsync(
                ServerListenOptions options,
                IRemoteClientListenerHandler handler,
                CancellationToken cancellationToken)
            {
                Options = options;
                Handler = handler;
                IsListening = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                StopCalled = true;
                IsListening = false;
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingLifecycleSink : IClientConnectionLifecycleSink
        {
            public List<ClientConnectionLifecycleEvent> Events { get; } = new List<ClientConnectionLifecycleEvent>();

            public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
            {
                Events.Add(lifecycleEvent);
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingMessageSink : IRemoteClientMessageSink
        {
            public IRemoteClientConnection Connection { get; private set; }

            public IMessage Message { get; private set; }

            public Task HandleAsync(
                IRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                Connection = connection;
                Message = message;
                return Task.CompletedTask;
            }
        }

        private sealed class TestRemoteClientConnection : IRemoteClientConnection
        {
            public TestRemoteClientConnection(string connectionId)
            {
                ConnectionId = connectionId;
                ConnectedAtUtc = DateTimeOffset.UtcNow;
                LastSeenUtc = ConnectedAtUtc;
            }

            public string ConnectionId { get; }

            public string ClientId => Identification == null ? null : Identification.Id;

            public ClientIdentification Identification { get; private set; }

            public bool IsIdentified => Identification != null;

            public bool IsConnected => !DisconnectCalled;

            public DateTimeOffset ConnectedAtUtc { get; }

            public DateTimeOffset LastSeenUtc { get; }

            public bool DisconnectCalled { get; private set; }

            public string DisconnectReason { get; private set; }

            public List<IMessage> SentMessages { get; } = new List<IMessage>();

            public void SetIdentification(ClientIdentification identification)
            {
                Identification = identification;
            }

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                SentMessages.Add(message);
                return Task.CompletedTask;
            }

            public Task DisconnectAsync(string reason, CancellationToken cancellationToken)
            {
                DisconnectCalled = true;
                DisconnectReason = reason;
                return Task.CompletedTask;
            }
        }
    }
}
