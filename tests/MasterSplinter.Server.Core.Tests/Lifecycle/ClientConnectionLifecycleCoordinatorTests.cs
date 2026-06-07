using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Lifecycle
{
    [TestClass]
    public class ClientConnectionLifecycleCoordinatorTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public async Task ConnectedWritesTransportLifecycleEvent()
        {
            var sink = new RecordingLifecycleSink();
            var coordinator = new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry(), sink);

            await coordinator.ConnectedAsync("connection-1", CancellationToken.None);

            Assert.AreEqual(1, sink.Events.Count);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Connected, sink.Events[0].Kind);
            Assert.AreEqual("connection-1", sink.Events[0].ConnectionId);
            Assert.IsNull(sink.Events[0].ClientId);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task IdentifiedAddsSessionAndWritesLifecycleEvent()
        {
            var registry = new ClientSessionRegistry();
            var sink = new RecordingLifecycleSink();
            var coordinator = new ClientConnectionLifecycleCoordinator(registry, sink);
            var session = new TestRemoteClientSession("client-1");

            await coordinator.IdentifiedAsync("connection-1", session, CancellationToken.None);

            Assert.IsTrue(registry.TryGet("client-1", out IRemoteClientSession storedSession));
            Assert.AreSame(session, storedSession);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Identified, sink.Events[0].Kind);
            Assert.AreEqual("connection-1", sink.Events[0].ConnectionId);
            Assert.AreEqual("client-1", sink.Events[0].ClientId);
            Assert.AreSame(session.Identification, sink.Events[0].Identification);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task DisconnectedRemovesSessionAndWritesLifecycleEvent()
        {
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(new TestRemoteClientSession("client-1"));
            var sink = new RecordingLifecycleSink();
            var coordinator = new ClientConnectionLifecycleCoordinator(registry, sink);

            await coordinator.DisconnectedAsync("connection-1", "client-1", "closed", CancellationToken.None);

            Assert.IsFalse(registry.TryGet("client-1", out _));
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Disconnected, sink.Events[0].Kind);
            Assert.AreEqual("closed", sink.Events[0].Reason);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task FaultedRemovesSessionAndWritesExceptionLifecycleEvent()
        {
            var exception = new InvalidOperationException("network failed");
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(new TestRemoteClientSession("client-1"));
            var sink = new RecordingLifecycleSink();
            var coordinator = new ClientConnectionLifecycleCoordinator(registry, sink);

            await coordinator.FaultedAsync("connection-1", "client-1", exception, CancellationToken.None);

            Assert.IsFalse(registry.TryGet("client-1", out _));
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Faulted, sink.Events[0].Kind);
            Assert.AreSame(exception, sink.Events[0].Exception);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task DisconnectedBeforeIdentificationDoesNotRequireClientId()
        {
            var sink = new RecordingLifecycleSink();
            var coordinator = new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry(), sink);

            await coordinator.DisconnectedAsync("connection-1", null, "closed before handshake", CancellationToken.None);

            Assert.AreEqual(ClientConnectionLifecycleEventKind.Disconnected, sink.Events[0].Kind);
            Assert.IsNull(sink.Events[0].ClientId);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task CancellationTokenFlowsToSink()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var sink = new RecordingLifecycleSink();
                var coordinator = new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry(), sink);

                await coordinator.ConnectedAsync("connection-1", tokenSource.Token);

                Assert.AreEqual(tokenSource.Token, sink.CancellationToken);
            }
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task InvalidInputsAreRejected()
        {
            var coordinator = new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry());

            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                coordinator.ConnectedAsync("", CancellationToken.None));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                coordinator.IdentifiedAsync("connection-1", null, CancellationToken.None));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                coordinator.FaultedAsync("connection-1", "client-1", null, CancellationToken.None));
        }

        private sealed class RecordingLifecycleSink : IClientConnectionLifecycleSink
        {
            public List<ClientConnectionLifecycleEvent> Events { get; } = new List<ClientConnectionLifecycleEvent>();

            public CancellationToken CancellationToken { get; private set; }

            public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
            {
                Events.Add(lifecycleEvent);
                CancellationToken = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private sealed class TestRemoteClientSession : IRemoteClientSession
        {
            public TestRemoteClientSession(string clientId)
            {
                ClientId = clientId;
                Identification = new ClientIdentification
                {
                    Id = clientId,
                    Username = "test-user",
                    PcName = "test-machine"
                };
                ConnectedAtUtc = DateTimeOffset.UtcNow;
                LastSeenUtc = ConnectedAtUtc;
            }

            public string ClientId { get; }

            public ClientIdentification Identification { get; }

            public bool IsConnected => true;

            public DateTimeOffset ConnectedAtUtc { get; }

            public DateTimeOffset LastSeenUtc { get; }

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
