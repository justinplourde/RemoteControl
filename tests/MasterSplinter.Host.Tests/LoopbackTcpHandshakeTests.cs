using MasterSplinter.Client.Core.Connections;
using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using MasterSplinter.Client.Core.Identity;
using MasterSplinter.Client.Core.Processes;
using MasterSplinter.Client.Core.Startup;
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
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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
        public async Task LoopbackTcpClientCompletesTlsHandshakeWhenServerCertificateMatches()
        {
            using (X509Certificate2 serverCertificate = CreateTestCertificate("matching-server"))
            {
                int port = GetFreeLoopbackPort();
                var registry = new ClientSessionRegistry();
                var lifecycleSink = new CompletionLifecycleSink();
                var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
                var handshake = new ClientHandshakeCoordinator(lifecycle);
                var listener = new LoopbackTcpRemoteClientListener();
                var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake);

                await orchestrator.StartAsync(
                    new ServerListenOptions("127.0.0.1", port, serverCertificate),
                    CancellationToken.None);
                try
                {
                    using (var client = new TcpClient())
                    {
                        await client.ConnectAsync(IPAddress.Loopback, port);
                        using (Stream stream = await CreateAuthenticatedClientStreamAsync(
                            client,
                            serverCertificate,
                            CancellationToken.None))
                        {
                            using (var writer = new PayloadWriter(stream, true))
                            {
                                writer.WriteMessage(CreateIdentification(ValidClientId));
                            }

                            using (var reader = new PayloadReader(stream, true))
                            {
                                var result = (ClientIdentificationResult)reader.ReadMessage();
                                Assert.IsTrue(result.Result);
                            }

                            await lifecycleSink.Identified.Task.WaitAsync(TimeSpan.FromSeconds(15));

                            Assert.IsTrue(registry.TryGet(ValidClientId, out _));
                        }
                    }
                }
                finally
                {
                    await orchestrator.StopAsync(CancellationToken.None);
                }
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientRejectsTlsHandshakeWhenServerCertificateDiffers()
        {
            using (X509Certificate2 serverCertificate = CreateTestCertificate("actual-server"))
            using (X509Certificate2 expectedCertificate = CreateTestCertificate("expected-server"))
            {
                int port = GetFreeLoopbackPort();
                var registry = new ClientSessionRegistry();
                var lifecycle = new ClientConnectionLifecycleCoordinator(registry);
                var handshake = new ClientHandshakeCoordinator(lifecycle);
                var listener = new LoopbackTcpRemoteClientListener();
                var orchestrator = new RemoteClientListenerOrchestrator(listener, lifecycle, handshake);

                await orchestrator.StartAsync(
                    new ServerListenOptions("127.0.0.1", port, serverCertificate),
                    CancellationToken.None);
                try
                {
                    await Assert.ThrowsExceptionAsync<AuthenticationException>(() =>
                        new LoopbackTcpHandshakeClient().IdentifyAsync(
                            "127.0.0.1",
                            port,
                            CreateIdentification(ValidClientId),
                            expectedCertificate,
                            CancellationToken.None));

                    Assert.IsFalse(registry.TryGet(ValidClientId, out _));
                }
                finally
                {
                    await orchestrator.StopAsync(CancellationToken.None);
                }
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
        public async Task LoopbackTcpClientHandlesGetDirectoryCommandAndReturnsDirectoryResponse()
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
                .AddHandler(new ResponseMessageHandlerAdapter<GetDirectory>(
                    new GetDirectoryHandler(new TestDirectoryProvider())))
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
                    new GetDirectory { RemotePath = "Z:\\Work" },
                    CancellationToken.None);

                Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                IMessage response = await responseSink.Message.Task.WaitAsync(TimeSpan.FromSeconds(15));
                ClientIdentificationResult clientResult = await clientTask.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(clientResult.Result);
                Assert.IsInstanceOfType(response, typeof(GetDirectoryResponse));

                var directory = (GetDirectoryResponse)response;
                Assert.AreEqual("Z:\\Work", directory.RemotePath);
                Assert.AreEqual("reports", directory.Items[0].Name);
                Assert.AreEqual("notes.txt", directory.Items[1].Name);
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientHandlesGetProcessesCommandAndReturnsProcessesResponse()
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
                .AddHandler(new ResponseMessageHandlerAdapter<GetProcesses>(
                    new GetProcessesHandler(new TestProcessProvider())))
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
                    new GetProcesses(),
                    CancellationToken.None);

                Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                IMessage response = await responseSink.Message.Task.WaitAsync(TimeSpan.FromSeconds(15));
                ClientIdentificationResult clientResult = await clientTask.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(clientResult.Result);
                Assert.IsInstanceOfType(response, typeof(GetProcessesResponse));

                var processes = (GetProcessesResponse)response;
                Assert.AreEqual("notepad.exe", processes.Processes[0].Name);
                Assert.AreEqual(42, processes.Processes[0].Id);
                Assert.AreEqual("notes.txt", processes.Processes[0].MainWindowTitle);
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientHandlesGetStartupItemsCommandAndReturnsStartupItemsResponse()
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
                .AddHandler(new ResponseMessageHandlerAdapter<GetStartupItems>(
                    new GetStartupItemsHandler(new TestStartupItemProvider())))
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
                    new GetStartupItems(),
                    CancellationToken.None);

                Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                IMessage response = await responseSink.Message.Task.WaitAsync(TimeSpan.FromSeconds(15));
                ClientIdentificationResult clientResult = await clientTask.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(clientResult.Result);
                Assert.IsInstanceOfType(response, typeof(GetStartupItemsResponse));

                var startupItems = (GetStartupItemsResponse)response;
                Assert.AreEqual("Agent", startupItems.StartupItems[0].Name);
                Assert.AreEqual("C:\\Tools\\agent.exe", startupItems.StartupItems[0].Path);
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None);
            }
        }

        [TestMethod, TestCategory("Host")]
        public async Task LoopbackTcpClientHandlesGetConnectionsCommandAndReturnsConnectionsResponse()
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
                .AddHandler(new ResponseMessageHandlerAdapter<GetConnections>(
                    new GetConnectionsHandler(new TestConnectionProvider())))
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
                    new GetConnections(),
                    CancellationToken.None);

                Assert.AreEqual(CommandDispatchStatus.Sent, dispatchResult.Status);

                IMessage response = await responseSink.Message.Task.WaitAsync(TimeSpan.FromSeconds(15));
                ClientIdentificationResult clientResult = await clientTask.WaitAsync(TimeSpan.FromSeconds(15));

                Assert.IsTrue(clientResult.Result);
                Assert.IsInstanceOfType(response, typeof(GetConnectionsResponse));

                var connections = (GetConnectionsResponse)response;
                Assert.AreEqual("browser", connections.Connections[0].ProcessName);
                Assert.AreEqual("127.0.0.1", connections.Connections[0].LocalAddress);
                Assert.AreEqual((ushort)5000, connections.Connections[0].LocalPort);
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

        private static async Task<Stream> CreateAuthenticatedClientStreamAsync(
            TcpClient client,
            X509Certificate2 expectedServerCertificate,
            CancellationToken cancellationToken)
        {
            var stream = new SslStream(
                client.GetStream(),
                false,
                (sender, certificate, chain, errors) => expectedServerCertificate.Equals(certificate));
            try
            {
                await stream.AuthenticateAsClientAsync(
                    IPAddress.Loopback.ToString(),
                    null,
                    SslProtocols.Tls12,
                    false).WaitAsync(cancellationToken);
                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private static X509Certificate2 CreateTestCertificate(string subjectName)
        {
            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(
                    "CN=" + subjectName,
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
                request.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") },
                        false));

                using (X509Certificate2 certificate = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddMinutes(-1),
                    DateTimeOffset.UtcNow.AddDays(1)))
                {
                    return X509CertificateLoader.LoadPkcs12(
                        certificate.Export(X509ContentType.Pkcs12),
                        string.Empty,
                        X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
                }
            }
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

        private sealed class TestDirectoryProvider : IDirectoryProvider
        {
            public DirectoryListResult GetDirectory(string remotePath)
            {
                return DirectoryListResult.Success(
                    remotePath,
                    new[]
                    {
                        new FileSystemEntry
                        {
                            EntryType = Quasar.Common.Enums.FileType.Directory,
                            Name = "reports",
                            Size = 0,
                            LastAccessTimeUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
                        },
                        new FileSystemEntry
                        {
                            EntryType = Quasar.Common.Enums.FileType.File,
                            Name = "notes.txt",
                            Size = 128,
                            ContentType = Quasar.Common.Enums.ContentType.Text,
                            LastAccessTimeUtc = new DateTime(2026, 6, 1, 12, 1, 0, DateTimeKind.Utc)
                        }
                    });
            }
        }

        private sealed class TestProcessProvider : IProcessProvider
        {
            public Quasar.Common.Models.Process[] GetProcesses()
            {
                return new[]
                {
                    new Quasar.Common.Models.Process
                    {
                        Name = "notepad.exe",
                        Id = 42,
                        MainWindowTitle = "notes.txt"
                    }
                };
            }
        }

        private sealed class TestStartupItemProvider : IStartupItemProvider
        {
            public StartupItemsResult GetStartupItems()
            {
                return StartupItemsResult.Success(new List<StartupItem>
                {
                    new StartupItem
                    {
                        Name = "Agent",
                        Path = "C:\\Tools\\agent.exe",
                        Type = Quasar.Common.Enums.StartupType.CurrentUserRun
                    }
                });
            }
        }

        private sealed class TestConnectionProvider : IConnectionProvider
        {
            public TcpConnection[] GetConnections()
            {
                return new[]
                {
                    new TcpConnection
                    {
                        ProcessName = "browser",
                        LocalAddress = "127.0.0.1",
                        LocalPort = 5000,
                        RemoteAddress = "127.0.0.1",
                        RemotePort = 5001,
                        State = Quasar.Common.Enums.ConnectionState.Established
                    }
                };
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
