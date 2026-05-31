using LocationRemote.Server.Core.Auditing;
using LocationRemote.Server.Core.Sessions;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Commands
{
    public sealed class ServerCommandDispatcher : IServerCommandDispatcher
    {
        private readonly IServerAuditSink _auditSink;
        private readonly IClientSessionRegistry _sessions;

        public ServerCommandDispatcher(IClientSessionRegistry sessions)
            : this(sessions, NoOpServerAuditSink.Instance)
        {
        }

        public ServerCommandDispatcher(IClientSessionRegistry sessions, IServerAuditSink auditSink)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
        }

        public async Task<CommandDispatchResult> DispatchAsync(string clientId, IMessage message, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!_sessions.TryGet(clientId, out IRemoteClientSession session))
            {
                await WriteAuditAsync(clientId, message, CommandDispatchStatus.ClientNotFound, null, cancellationToken)
                    .ConfigureAwait(false);
                return CommandDispatchResult.ClientNotFound();
            }

            try
            {
                await session.SendAsync(message, cancellationToken).ConfigureAwait(false);
                await WriteAuditAsync(clientId, message, CommandDispatchStatus.Sent, null, cancellationToken)
                    .ConfigureAwait(false);
                return CommandDispatchResult.Sent();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await WriteAuditAsync(clientId, message, CommandDispatchStatus.Faulted, exception, CancellationToken.None)
                    .ConfigureAwait(false);
                return CommandDispatchResult.Faulted(exception);
            }
        }

        private Task WriteAuditAsync(
            string clientId,
            IMessage message,
            CommandDispatchStatus status,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var auditEvent = new ServerAuditEvent(
                DateTimeOffset.UtcNow,
                clientId,
                message.GetType().FullName,
                status.ToString(),
                exception == null ? null : exception.Message);

            return _auditSink.WriteAsync(auditEvent, cancellationToken);
        }
    }
}
