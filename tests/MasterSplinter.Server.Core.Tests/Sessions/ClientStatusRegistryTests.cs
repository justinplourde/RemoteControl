using MasterSplinter.Common.Enums;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace MasterSplinter.Server.Core.Tests.Sessions
{
    [TestClass]
    public class ClientStatusRegistryTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public void SetStatusStoresLatestStatusMessage()
        {
            var registry = new ClientStatusRegistry();

            registry.SetStatus("client-1", "Ready");
            registry.SetStatus("client-1", "Working");

            Assert.IsTrue(registry.TryGet("client-1", out ClientStatusSnapshot snapshot));
            Assert.AreEqual("Working", snapshot.StatusMessage);
            Assert.IsFalse(snapshot.UserStatus.HasValue);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void SetUserStatusPreservesStatusMessage()
        {
            var registry = new ClientStatusRegistry();

            registry.SetStatus("client-1", "Ready");
            registry.SetUserStatus("CLIENT-1", UserStatus.Idle);

            Assert.IsTrue(registry.TryGet("client-1", out ClientStatusSnapshot snapshot));
            Assert.AreEqual("Ready", snapshot.StatusMessage);
            Assert.AreEqual(UserStatus.Idle, snapshot.UserStatus);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void EmptyClientIdIsRejected()
        {
            var registry = new ClientStatusRegistry();

            Assert.ThrowsException<ArgumentException>(() => registry.SetStatus("", "Ready"));
            Assert.ThrowsException<ArgumentException>(() => registry.SetUserStatus(" ", UserStatus.Active));
            Assert.ThrowsException<ArgumentException>(() => registry.TryGet(null, out _));
        }
    }
}
