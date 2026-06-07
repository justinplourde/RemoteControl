using MasterSplinter.Client.Core.Identity;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Parity
{
    [TestClass]
    public class InMemoryHandshakeParityTests
    {
        private const string ValidClientId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        [TestMethod, TestCategory("Parity")]
        public async Task ModernClientIdentificationCompletesServerHandshakeInMemory()
        {
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new RecordingLifecycleSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new InMemoryRemoteClientListener();
            var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake);
            await orchestrator.StartAsync(new ServerListenOptions("memory", 4782), CancellationToken.None);
            var client = new InMemoryRemoteClientConnection("connection-1");

            await listener.ConnectAsync(client, CancellationToken.None);
            await listener.ReceiveAsync(client, CreateModernIdentification(), CancellationToken.None);

            Assert.IsTrue(registry.TryGet(ValidClientId, out IRemoteClientSession session));
            Assert.AreSame(client, session);
            Assert.AreEqual(2, lifecycleSink.Events.Count);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Connected, lifecycleSink.Events[0].Kind);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Identified, lifecycleSink.Events[1].Kind);
            Assert.AreEqual(1, client.SentMessages.Count);
            Assert.IsInstanceOfType(client.SentMessages[0], typeof(ClientIdentificationResult));
            Assert.IsTrue(((ClientIdentificationResult)client.SentMessages[0]).Result);
        }

        [TestMethod, TestCategory("Parity")]
        public async Task InvalidModernClientIdentificationFailsHandshakeInMemory()
        {
            var registry = new ClientSessionRegistry();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new InMemoryRemoteClientListener();
            var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake);
            await orchestrator.StartAsync(new ServerListenOptions("memory", 4782), CancellationToken.None);
            var client = new InMemoryRemoteClientConnection("connection-1");
            ClientIdentification identification = CreateModernIdentification();
            identification.Id = "too-short";

            await listener.ConnectAsync(client, CancellationToken.None);
            await listener.ReceiveAsync(client, identification, CancellationToken.None);

            Assert.AreEqual(0, registry.Count);
            Assert.IsTrue(client.DisconnectCalled);
            Assert.AreEqual("Client id must be 64 characters.", client.DisconnectReason);
            Assert.IsFalse(((ClientIdentificationResult)client.SentMessages[0]).Result);
        }

        private static ClientIdentification CreateModernIdentification()
        {
            var capabilities = new ClientCapabilities();
            capabilities.SupportedFeatures.Add("handshake");
            capabilities.SupportedFeatures.Add("message.dispatch");

            var options = new ClientIdentityOptions(
                "modern-dev",
                "Test OS",
                "User",
                "Unknown",
                "XX",
                0,
                ValidClientId,
                "test-user",
                "test-machine",
                "modern",
                "dev-key",
                new byte[] { 1, 2, 3, 4 },
                new ProtocolVersion { Major = 1, Minor = 0 },
                capabilities);

            return new ClientIdentificationFactory().Create(options);
        }

        private sealed class InMemoryRemoteClientListener : IRemoteClientListener
        {
            private IRemoteClientListenerHandler _handler;

            public bool IsListening { get; private set; }

            public ServerListenOptions Options { get; private set; }

            public Task StartAsync(
                ServerListenOptions options,
                IRemoteClientListenerHandler handler,
                CancellationToken cancellationToken)
            {
                Options = options;
                _handler = handler;
                IsListening = true;
                return Task.CompletedTask;
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                IsListening = false;
                return Task.CompletedTask;
            }

            public Task ConnectAsync(InMemoryRemoteClientConnection connection, CancellationToken cancellationToken)
            {
                return _handler.ClientConnectedAsync(connection, cancellationToken);
            }

            public Task ReceiveAsync(
                InMemoryRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                return _handler.MessageReceivedAsync(connection, message, cancellationToken);
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

        private sealed class InMemoryRemoteClientConnection : IRemoteClientConnection
        {
            public InMemoryRemoteClientConnection(string connectionId)
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

            public List<IMessage> SentMessages { get; } = new List<IMessage>();

            public bool DisconnectCalled { get; private set; }

            public string DisconnectReason { get; private set; }

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
