using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Sessions
{
    [TestClass]
    public class ClientSessionRegistryTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public void AddOrUpdateStoresSessionByClientId()
        {
            var registry = new ClientSessionRegistry();
            var session = new TestRemoteClientSession("client-1");

            registry.AddOrUpdate(session);

            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGet("client-1", out IRemoteClientSession storedSession));
            Assert.AreSame(session, storedSession);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void AddOrUpdateReplacesExistingSessionCaseInsensitively()
        {
            var registry = new ClientSessionRegistry();
            var first = new TestRemoteClientSession("client-1");
            var second = new TestRemoteClientSession("CLIENT-1");

            registry.AddOrUpdate(first);
            registry.AddOrUpdate(second);

            Assert.AreEqual(1, registry.Count);
            Assert.IsTrue(registry.TryGet("client-1", out IRemoteClientSession storedSession));
            Assert.AreSame(second, storedSession);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void RemoveDeletesRegisteredSession()
        {
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(new TestRemoteClientSession("client-1"));

            Assert.IsTrue(registry.Remove("client-1"));

            Assert.AreEqual(0, registry.Count);
            Assert.IsFalse(registry.TryGet("client-1", out _));
        }

        [TestMethod, TestCategory("ServerCore")]
        public void GetSnapshotsReturnsDefensiveCopy()
        {
            var registry = new ClientSessionRegistry();
            var session = new TestRemoteClientSession("client-1");
            registry.AddOrUpdate(session);

            IReadOnlyList<ClientSessionSnapshot> firstSnapshot = registry.GetSnapshots();
            registry.Remove("client-1");
            IReadOnlyList<ClientSessionSnapshot> secondSnapshot = registry.GetSnapshots();

            Assert.AreEqual(1, firstSnapshot.Count);
            Assert.AreEqual("client-1", firstSnapshot[0].ClientId);
            Assert.AreSame(session.Identification, firstSnapshot[0].Identification);
            Assert.AreEqual(0, secondSnapshot.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void EmptyClientIdIsRejected()
        {
            var registry = new ClientSessionRegistry();

            Assert.ThrowsException<ArgumentException>(() => registry.TryGet("", out _));
            Assert.ThrowsException<ArgumentException>(() => registry.Remove(" "));
            Assert.ThrowsException<ArgumentException>(() => registry.AddOrUpdate(new TestRemoteClientSession(null)));
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
