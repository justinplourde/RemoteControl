using MasterSplinter.Server.Core.Auditing;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.Commands
{
    [TestClass]
    public class ServerCommandDispatcherTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public async Task DispatchSendsMessageToRegisteredClient()
        {
            var correlationId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var session = new RecordingRemoteClientSession("client-1");
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(session);
            var auditSink = new RecordingAuditSink();
            var dispatcher = new ServerCommandDispatcher(registry, auditSink);
            var message = new GetSystemInfo();
            var request = new CommandDispatchRequest(correlationId, "client-1", message, "operator-1", "unit-test");

            CommandDispatchResult result = await dispatcher.DispatchAsync(request, CancellationToken.None);

            Assert.AreEqual(correlationId, result.CorrelationId);
            Assert.AreEqual(CommandDispatchStatus.Sent, result.Status);
            Assert.AreSame(message, session.SentMessages[0]);
            Assert.AreEqual(correlationId, auditSink.Events[0].CorrelationId);
            Assert.AreEqual("operator-1", auditSink.Events[0].OperatorId);
            Assert.AreEqual("unit-test", auditSink.Events[0].Source);
            Assert.AreEqual("Sent", auditSink.Events[0].Outcome);
            Assert.AreEqual(typeof(GetSystemInfo).FullName, auditSink.Events[0].MessageType);
            Assert.AreEqual("ReadOnlyInventory", auditSink.Events[0].SafetyClass);
            Assert.IsFalse(auditSink.Events[0].RequiresPermission);
            Assert.IsFalse(auditSink.Events[0].RequiresConsent);
            Assert.AreEqual(CommandSafetyClass.ReadOnlyInventory, result.SafetyMetadata.SafetyClass);
            Assert.IsTrue(result.SafetyMetadata.IsReadOnly);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task ConvenienceDispatchGeneratesCorrelationId()
        {
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(new RecordingRemoteClientSession("client-1"));
            var auditSink = new RecordingAuditSink();
            var dispatcher = new ServerCommandDispatcher(registry, auditSink);

            CommandDispatchResult result = await dispatcher.DispatchAsync("client-1", new GetSystemInfo(), CancellationToken.None);

            Assert.AreNotEqual(Guid.Empty, result.CorrelationId);
            Assert.AreEqual(result.CorrelationId, auditSink.Events[0].CorrelationId);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task MissingClientReturnsClientNotFoundAndWritesAuditEvent()
        {
            var auditSink = new RecordingAuditSink();
            var dispatcher = new ServerCommandDispatcher(new ClientSessionRegistry(), auditSink);

            CommandDispatchResult result = await dispatcher.DispatchAsync("client-1", new GetSystemInfo(), CancellationToken.None);

            Assert.AreNotEqual(Guid.Empty, result.CorrelationId);
            Assert.AreEqual(CommandDispatchStatus.ClientNotFound, result.Status);
            Assert.AreEqual("ClientNotFound", auditSink.Events[0].Outcome);
            Assert.AreEqual(result.CorrelationId, auditSink.Events[0].CorrelationId);
            Assert.AreEqual(CommandSafetyClass.ReadOnlyInventory, result.SafetyMetadata.SafetyClass);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task SendExceptionReturnsFaultedAndWritesAuditEvent()
        {
            var exception = new InvalidOperationException("send failed");
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(new ThrowingRemoteClientSession("client-1", exception));
            var auditSink = new RecordingAuditSink();
            var dispatcher = new ServerCommandDispatcher(registry, auditSink);

            CommandDispatchResult result = await dispatcher.DispatchAsync("client-1", new GetSystemInfo(), CancellationToken.None);

            Assert.AreNotEqual(Guid.Empty, result.CorrelationId);
            Assert.AreEqual(CommandDispatchStatus.Faulted, result.Status);
            Assert.AreSame(exception, result.Exception);
            Assert.AreEqual(result.CorrelationId, auditSink.Events[0].CorrelationId);
            Assert.AreEqual("Faulted", auditSink.Events[0].Outcome);
            Assert.AreEqual("send failed", auditSink.Events[0].ErrorMessage);
            Assert.AreEqual(CommandSafetyClass.ReadOnlyInventory, result.SafetyMetadata.SafetyClass);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task CancellationFromSendIsPropagatedWhenTokenIsCanceled()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();
                var registry = new ClientSessionRegistry();
                registry.AddOrUpdate(new CancelingRemoteClientSession("client-1"));
                var dispatcher = new ServerCommandDispatcher(registry);

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
                    dispatcher.DispatchAsync("client-1", new GetSystemInfo(), tokenSource.Token));
            }
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task DispatchRejectsInvalidInputs()
        {
            var dispatcher = new ServerCommandDispatcher(new ClientSessionRegistry());

            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                dispatcher.DispatchAsync("", new GetSystemInfo(), CancellationToken.None));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                dispatcher.DispatchAsync("client-1", null, CancellationToken.None));
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                dispatcher.DispatchAsync(null, CancellationToken.None));
            Assert.ThrowsException<ArgumentException>(() =>
                new CommandDispatchRequest(Guid.Empty, "client-1", new GetSystemInfo(), null, null));
        }

        private sealed class RecordingAuditSink : IServerAuditSink
        {
            public List<ServerAuditEvent> Events { get; } = new List<ServerAuditEvent>();

            public Task WriteAsync(ServerAuditEvent auditEvent, CancellationToken cancellationToken)
            {
                Events.Add(auditEvent);
                return Task.CompletedTask;
            }
        }

        private sealed class RecordingRemoteClientSession : RemoteClientSessionBase
        {
            public RecordingRemoteClientSession(string clientId)
                : base(clientId)
            {
            }

            public List<IMessage> SentMessages { get; } = new List<IMessage>();

            public override Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                SentMessages.Add(message);
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingRemoteClientSession : RemoteClientSessionBase
        {
            private readonly Exception _exception;

            public ThrowingRemoteClientSession(string clientId, Exception exception)
                : base(clientId)
            {
                _exception = exception;
            }

            public override Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                throw _exception;
            }
        }

        private sealed class CancelingRemoteClientSession : RemoteClientSessionBase
        {
            public CancelingRemoteClientSession(string clientId)
                : base(clientId)
            {
            }

            public override Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }

        private abstract class RemoteClientSessionBase : IRemoteClientSession
        {
            protected RemoteClientSessionBase(string clientId)
            {
                ClientId = clientId;
                Identification = new ClientIdentification { Id = clientId };
                ConnectedAtUtc = DateTimeOffset.UtcNow;
                LastSeenUtc = ConnectedAtUtc;
            }

            public string ClientId { get; }

            public ClientIdentification Identification { get; }

            public bool IsConnected => true;

            public DateTimeOffset ConnectedAtUtc { get; }

            public DateTimeOffset LastSeenUtc { get; }

            public abstract Task SendAsync(IMessage message, CancellationToken cancellationToken);
        }
    }
}
