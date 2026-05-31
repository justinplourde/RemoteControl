using LocationRemote.Server.Core.Auditing;
using LocationRemote.Server.Core.Commands;
using LocationRemote.Server.Core.Sessions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Tests.Commands
{
    [TestClass]
    public class ServerCommandDispatcherTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public async Task DispatchSendsMessageToRegisteredClient()
        {
            var session = new RecordingRemoteClientSession("client-1");
            var registry = new ClientSessionRegistry();
            registry.AddOrUpdate(session);
            var auditSink = new RecordingAuditSink();
            var dispatcher = new ServerCommandDispatcher(registry, auditSink);
            var message = new GetSystemInfo();

            CommandDispatchResult result = await dispatcher.DispatchAsync("client-1", message, CancellationToken.None);

            Assert.AreEqual(CommandDispatchStatus.Sent, result.Status);
            Assert.AreSame(message, session.SentMessages[0]);
            Assert.AreEqual("Sent", auditSink.Events[0].Outcome);
            Assert.AreEqual(typeof(GetSystemInfo).FullName, auditSink.Events[0].MessageType);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task MissingClientReturnsClientNotFoundAndWritesAuditEvent()
        {
            var auditSink = new RecordingAuditSink();
            var dispatcher = new ServerCommandDispatcher(new ClientSessionRegistry(), auditSink);

            CommandDispatchResult result = await dispatcher.DispatchAsync("client-1", new GetSystemInfo(), CancellationToken.None);

            Assert.AreEqual(CommandDispatchStatus.ClientNotFound, result.Status);
            Assert.AreEqual("ClientNotFound", auditSink.Events[0].Outcome);
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

            Assert.AreEqual(CommandDispatchStatus.Faulted, result.Status);
            Assert.AreSame(exception, result.Exception);
            Assert.AreEqual("Faulted", auditSink.Events[0].Outcome);
            Assert.AreEqual("send failed", auditSink.Events[0].ErrorMessage);
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
