using MasterSplinter.Client.Core.Identity;
using MasterSplinter.Client.Host;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using Quasar.Common.Protocol;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Host.Tests
{
    [TestClass]
    public class LoopbackTcpHandshakeTests
    {
        private const string ValidClientId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientCompletesHandshakeWithLoopbackTcpServer()
        {
            int port = GetFreeLoopbackPort();
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new CompletionLifecycleSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake);

            await orchestrator.StartAsync(new ServerListenOptions("127.0.0.1", port), CancellationToken.None);
            try
            {
                ClientIdentificationResult result = await new LoopbackTcpHandshakeClient()
                    .IdentifyAsync("127.0.0.1", port, CreateIdentification(ValidClientId), CancellationToken.None);

                await lifecycleSink.Identified.Task.WaitAsync(TimeSpan.FromSeconds(5));

                Assert.IsTrue(result.Result);
                Assert.IsTrue(registry.TryGet(ValidClientId, out _));
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpServerRejectsNonLoopbackBindAddress()
        {
            var listener = new LoopbackTcpRemoteClientListener();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                listener.StartAsync(
                    new ServerListenOptions("0.0.0.0", 4782),
                    new NoOpListenerHandler(),
                    CancellationToken.None));
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientRejectsNonLoopbackTargetAddress()
        {
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                new LoopbackTcpHandshakeClient().IdentifyAsync(
                    "8.8.8.8",
                    4782,
                    CreateIdentification(ValidClientId),
                    CancellationToken.None));
        }

        private static ClientIdentification CreateIdentification(string clientId)
        {
            var capabilities = new ClientCapabilities();
            capabilities.SupportedFeatures.Add("handshake");

            return new ClientIdentificationFactory().Create(new ClientIdentityOptions(
                "modern-dev",
                "Test OS",
                "User",
                "Unknown",
                "XX",
                0,
                clientId,
                "test-user",
                "test-machine",
                "modern",
                "dev-key",
                new byte[] { 1, 2, 3, 4 },
                new ProtocolVersion { Major = 1, Minor = 0 },
                capabilities));
        }

        private static int GetFreeLoopbackPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private sealed class CompletionLifecycleSink : IClientConnectionLifecycleSink
        {
            public TaskCompletionSource<bool> Identified { get; } = new TaskCompletionSource<bool>();

            public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
            {
                if (lifecycleEvent.Kind == ClientConnectionLifecycleEventKind.Identified)
                    Identified.TrySetResult(true);

                return Task.CompletedTask;
            }
        }

        private sealed class NoOpListenerHandler : IRemoteClientListenerHandler
        {
            public Task ClientConnectedAsync(IRemoteClientConnection connection, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task MessageReceivedAsync(IRemoteClientConnection connection, IMessage message, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task ClientDisconnectedAsync(IRemoteClientConnection connection, string reason, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task ClientFaultedAsync(IRemoteClientConnection connection, Exception exception, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
