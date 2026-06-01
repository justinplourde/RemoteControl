using MasterSplinter.Server.Core.Auditing;
using MasterSplinter.Server.Core.Sessions;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Commands
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
            return await DispatchAsync(CommandDispatchRequest.Create(clientId, message), cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<CommandDispatchResult> DispatchAsync(CommandDispatchRequest request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_sessions.TryGet(request.ClientId, out IRemoteClientSession session))
            {
                await WriteAuditAsync(request, CommandDispatchStatus.ClientNotFound, null, cancellationToken)
                    .ConfigureAwait(false);
                return CommandDispatchResult.ClientNotFound(request.CorrelationId);
            }

            try
            {
                await session.SendAsync(request.Message, cancellationToken).ConfigureAwait(false);
                await WriteAuditAsync(request, CommandDispatchStatus.Sent, null, cancellationToken)
                    .ConfigureAwait(false);
                return CommandDispatchResult.Sent(request.CorrelationId);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await WriteAuditAsync(request, CommandDispatchStatus.Faulted, exception, CancellationToken.None)
                    .ConfigureAwait(false);
                return CommandDispatchResult.Faulted(request.CorrelationId, exception);
            }
        }

        private Task WriteAuditAsync(
            CommandDispatchRequest request,
            CommandDispatchStatus status,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var auditEvent = new ServerAuditEvent(
                DateTimeOffset.UtcNow,
                request.CorrelationId,
                request.ClientId,
                request.OperatorId,
                request.Source,
                request.MessageType,
                status.ToString(),
                exception == null ? null : exception.Message);

            return _auditSink.WriteAsync(auditEvent, cancellationToken);
        }
    }
}
