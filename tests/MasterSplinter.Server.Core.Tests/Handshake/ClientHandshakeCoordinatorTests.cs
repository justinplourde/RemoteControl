using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using Quasar.Common.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Handshake
{
    [TestClass]
    public class ClientHandshakeCoordinatorTests
    {
        private const string ValidClientId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

        [TestMethod, TestCategory("ServerCore")]
        public async Task AcceptedIdentificationRegistersSessionAndReturnsSuccessfulResult()
        {
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new RecordingLifecycleSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var coordinator = new ClientHandshakeCoordinator(lifecycle);
            var identification = CreateIdentification(ValidClientId);
            var session = new TestRemoteClientSession(identification);

            ClientHandshakeResult result = await coordinator.IdentifyAsync("connection-1", session, CancellationToken.None);

            Assert.IsTrue(result.Accepted);
            Assert.IsTrue(result.Response.Result);
            Assert.AreEqual(ValidClientId, result.ClientId);
            Assert.IsTrue(registry.TryGet(ValidClientId, out IRemoteClientSession storedSession));
            Assert.AreSame(session, storedSession);
            Assert.AreEqual(ClientConnectionLifecycleEventKind.Identified, lifecycleSink.Events[0].Kind);
            Assert.AreSame(identification, lifecycleSink.Events[0].Identification);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task AcceptedIdentificationPreservesProtocolMetadataForCapabilityNegotiation()
        {
            var coordinator = new ClientHandshakeCoordinator(
                new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry()));
            var capabilities = new ClientCapabilities
            {
                SupportedFeatures = new List<string> { "filesystem.read", "system.info" }
            };
            var protocolVersion = new ProtocolVersion { Major = 1, Minor = 2 };
            var identification = CreateIdentification(ValidClientId);
            identification.ProtocolVersion = protocolVersion;
            identification.Capabilities = capabilities;

            ClientHandshakeResult result = await coordinator.IdentifyAsync(
                "connection-1",
                new TestRemoteClientSession(identification),
                CancellationToken.None);

            Assert.IsTrue(result.Accepted);
            Assert.AreSame(protocolVersion, result.ProtocolVersion);
            Assert.AreSame(capabilities, result.Capabilities);
            CollectionAssert.Contains(result.Capabilities.SupportedFeatures, "filesystem.read");
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task LegacyIdentificationWithoutProtocolMetadataIsAccepted()
        {
            var coordinator = new ClientHandshakeCoordinator(
                new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry()));
            var identification = CreateIdentification(ValidClientId);
            identification.ProtocolVersion = null;
            identification.Capabilities = null;

            ClientHandshakeResult result = await coordinator.IdentifyAsync(
                "connection-1",
                new TestRemoteClientSession(identification),
                CancellationToken.None);

            Assert.IsTrue(result.Accepted);
            Assert.IsNull(result.ProtocolVersion);
            Assert.IsNull(result.Capabilities);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task InvalidLegacyClientIdIsRejectedAndDoesNotRegisterSession()
        {
            var registry = new ClientSessionRegistry();
            var lifecycleSink = new RecordingLifecycleSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
            var coordinator = new ClientHandshakeCoordinator(lifecycle);

            ClientHandshakeResult result = await coordinator.IdentifyAsync(
                "connection-1",
                new TestRemoteClientSession(CreateIdentification("too-short")),
                CancellationToken.None);

            Assert.IsFalse(result.Accepted);
            Assert.IsFalse(result.Response.Result);
            Assert.AreEqual("too-short", result.ClientId);
            Assert.AreEqual("Client id must be 64 characters.", result.RejectionReason);
            Assert.AreEqual(0, registry.Count);
            Assert.AreEqual(0, lifecycleSink.Events.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task CustomValidatorCanRejectIdentificationBeforeLifecycleRegistration()
        {
            var registry = new ClientSessionRegistry();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry);
            var coordinator = new ClientHandshakeCoordinator(
                lifecycle,
                new RejectingValidator("certificate signature failed"));

            ClientHandshakeResult result = await coordinator.IdentifyAsync(
                "connection-1",
                new TestRemoteClientSession(CreateIdentification(ValidClientId)),
                CancellationToken.None);

            Assert.IsFalse(result.Accepted);
            Assert.AreEqual("certificate signature failed", result.RejectionReason);
            Assert.AreEqual(0, registry.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task CancellationTokenFlowsToLifecycleCoordinator()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var lifecycleSink = new RecordingLifecycleSink();
                var lifecycle = new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry(), lifecycleSink);
                var coordinator = new ClientHandshakeCoordinator(lifecycle);

                await coordinator.IdentifyAsync(
                    "connection-1",
                    new TestRemoteClientSession(CreateIdentification(ValidClientId)),
                    tokenSource.Token);

                Assert.AreEqual(tokenSource.Token, lifecycleSink.CancellationToken);
            }
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task InvalidInputsAreRejected()
        {
            var coordinator = new ClientHandshakeCoordinator(
                new ClientConnectionLifecycleCoordinator(new ClientSessionRegistry()));

            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                coordinator.IdentifyAsync("", new TestRemoteClientSession(CreateIdentification(ValidClientId)), CancellationToken.None));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                coordinator.IdentifyAsync("connection-1", null, CancellationToken.None));
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

        private sealed class RejectingValidator : IClientIdentificationValidator
        {
            private readonly string _reason;

            public RejectingValidator(string reason)
            {
                _reason = reason;
            }

            public ClientIdentificationValidationResult Validate(ClientIdentification identification)
            {
                return ClientIdentificationValidationResult.Reject(_reason);
            }
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
            public TestRemoteClientSession(ClientIdentification identification)
            {
                Identification = identification;
                ClientId = identification == null ? null : identification.Id;
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
