using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using MasterSplinter.Client.Core.Identity;
using MasterSplinter.Client.Core.SystemInformation;
using MasterSplinter.Client.Host;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using Quasar.Common.Models;
using Quasar.Common.Networking;
using Quasar.Common.Protocol;
using System;
using System.Collections.Generic;
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

                await lifecycleSink.Identified.Task.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(result.Result);
                Assert.IsTrue(registry.TryGet(ValidClientId, out _));
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task ServerCommandDispatcherSendsCommandOverLoopbackTcpSession()
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
                using (var client = new TcpClient())
                {
                    await client.ConnectAsync(IPAddress.Loopback, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        using (var writer = new PayloadWriter(stream, true))
                        {
                            writer.WriteMessage(CreateIdentification(ValidClientId));
                        }

                        using (var reader = new PayloadReader(stream, true))
                        {
                            var handshakeResult = (ClientIdentificationResult)reader.ReadMessage();
                            Assert.IsTrue(handshakeResult.Result);
                        }

                        await lifecycleSink.Identified.Task.WaitAsync(TimeSpan.FromSeconds(15));

                        var dispatcher = new ServerCommandDispatcher(registry);
                        CommandDispatchResult dispatchResult = await dispatcher.DispatchAsync(
                            ValidClientId,
                            new GetSystemInfo(),
                            CancellationToken.None);

                        Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                        using (var reader = new PayloadReader(stream, true))
                        {
                            IMessage command = reader.ReadMessage();
                            Assert.IsInstanceOfType(command, typeof(GetSystemInfo));
                        }
                    }
                }
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientHandlesServerCommandAndReturnsSystemInfoResponse()
        {
            int port = GetFreeLoopbackPort();
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new CompletionLifecycleSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var responseSink = new CompletionMessageSink();
            var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake, responseSink);
            MessageDispatcher dispatcher = new MessageDispatcher.Builder()
                .AddHandler(new ResponseMessageHandlerAdapter<GetSystemInfo>(
                    new GetSystemInfoHandler(new TestSystemInfoProvider())))
                .Build();

            await orchestrator.StartAsync(new ServerListenOptions("127.0.0.1", port), CancellationToken.None);
            try
            {
                Task<ClientIdentificationResult> clientTask = new LoopbackTcpCommandClient()
                    .IdentifyAndHandleOneCommandAsync(
                        "127.0.0.1",
                        port,
                        CreateIdentification(ValidClientId),
                        dispatcher,
                        CancellationToken.None);

                await lifecycleSink.Identified.Task.WaitAsync(TimeSpan.FromSeconds(15));

                var serverDispatcher = new ServerCommandDispatcher(registry);
                CommandDispatchResult dispatchResult = await serverDispatcher.DispatchAsync(
                    ValidClientId,
                    new GetSystemInfo(),
                    CancellationToken.None);

                Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                IMessage response = await responseSink.Message.Task.WaitAsync(TimeSpan.FromSeconds(15));
                ClientIdentificationResult clientResult = await clientTask.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(clientResult.Result);
                Assert.IsInstanceOfType(response, typeof(GetSystemInfoResponse));

                var systemInfo = (GetSystemInfoResponse)response;
                Assert.AreEqual("PC Name", systemInfo.SystemInfos[0].Item1);
                Assert.AreEqual("modern-client", systemInfo.SystemInfos[0].Item2);
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientHandlesGetDrivesCommandAndReturnsDrivesResponse()
        {
            int port = GetFreeLoopbackPort();
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new CompletionLifecycleSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var responseSink = new CompletionMessageSink();
            var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake, responseSink);
            MessageDispatcher dispatcher = new MessageDispatcher.Builder()
                .AddHandler(new ResponseMessageHandlerAdapter<GetDrives>(
                    new GetDrivesHandler(new TestDriveProvider())))
                .Build();

            await orchestrator.StartAsync(new ServerListenOptions("127.0.0.1", port), CancellationToken.None);
            try
            {
                Task<ClientIdentificationResult> clientTask = new LoopbackTcpCommandClient()
                    .IdentifyAndHandleOneCommandAsync(
                        "127.0.0.1",
                        port,
                        CreateIdentification(ValidClientId),
                        dispatcher,
                        CancellationToken.None);

                await lifecycleSink.Identified.Task.WaitAsync(TimeSpan.FromSeconds(15));

                var serverDispatcher = new ServerCommandDispatcher(registry);
                CommandDispatchResult dispatchResult = await serverDispatcher.DispatchAsync(
                    ValidClientId,
                    new GetDrives(),
                    CancellationToken.None);

                Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                IMessage response = await responseSink.Message.Task.WaitAsync(TimeSpan.FromSeconds(15));
                ClientIdentificationResult clientResult = await clientTask.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(clientResult.Result);
                Assert.IsInstanceOfType(response, typeof(GetDrivesResponse));

                var drives = (GetDrivesResponse)response;
                Assert.AreEqual("Z:\\ [Network Drive, NTFS]", drives.Drives[0].DisplayName);
                Assert.AreEqual("Z:\\", drives.Drives[0].RootDirectory);
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
            public TaskCompletionSource<bool> Identified { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
            {
                if (lifecycleEvent.Kind == ClientConnectionLifecycleEventKind.Identified)
                    Identified.TrySetResult(true);

                return Task.CompletedTask;
            }
        }

        private sealed class CompletionMessageSink : IRemoteClientMessageSink
        {
            public TaskCompletionSource<IMessage> Message { get; } =
                new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);

            public Task HandleAsync(
                IRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                Message.TrySetResult(message);
                return Task.CompletedTask;
            }
        }

        private sealed class TestSystemInfoProvider : ISystemInfoProvider
        {
            public IReadOnlyList<Tuple<string, string>> GetSystemInfo()
            {
                return new[]
                {
                    Tuple.Create("PC Name", "modern-client"),
                    Tuple.Create("Country", "XX")
                };
            }
        }

        private sealed class TestDriveProvider : IDriveProvider
        {
            public DriveListResult GetDrives()
            {
                return DriveListResult.Success(new[]
                {
                    new Drive { DisplayName = "Z:\\ [Network Drive, NTFS]", RootDirectory = "Z:\\" }
                });
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
