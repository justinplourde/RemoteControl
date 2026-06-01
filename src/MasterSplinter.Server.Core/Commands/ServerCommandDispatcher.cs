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
        private readonly ICommandSafetyClassifier _classifier;
        private readonly ICommandDispatchPolicy _policy;
        private readonly IClientSessionRegistry _sessions;

        public ServerCommandDispatcher(IClientSessionRegistry sessions)
            : this(sessions, NoOpServerAuditSink.Instance)
        {
        }

        public ServerCommandDispatcher(IClientSessionRegistry sessions, IServerAuditSink auditSink)
            : this(sessions, auditSink, DefaultCommandSafetyClassifier.Instance)
        {
        }

        public ServerCommandDispatcher(
            IClientSessionRegistry sessions,
            IServerAuditSink auditSink,
            ICommandSafetyClassifier classifier)
            : this(sessions, auditSink, classifier, DefaultCommandDispatchPolicy.Instance)
        {
        }

        public ServerCommandDispatcher(
            IClientSessionRegistry sessions,
            IServerAuditSink auditSink,
            ICommandSafetyClassifier classifier,
            ICommandDispatchPolicy policy)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _auditSink = auditSink ?? throw new ArgumentNullException(nameof(auditSink));
            _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
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

            CommandSafetyMetadata safetyMetadata = _classifier.Classify(request.Message);
            CommandDispatchPolicyDecision policyDecision = _policy.Authorize(request, safetyMetadata);
            if (!policyDecision.IsAllowed)
            {
                await WriteAuditAsync(
                    request,
                    safetyMetadata,
                    policyDecision.DeniedStatus,
                    policyDecision.Reason,
                    null,
                    cancellationToken).ConfigureAwait(false);
                return CommandDispatchResult.Denied(
                    request.CorrelationId,
                    policyDecision.DeniedStatus,
                    safetyMetadata,
                    policyDecision.Reason);
            }

            if (!_sessions.TryGet(request.ClientId, out IRemoteClientSession session))
            {
                await WriteAuditAsync(request, safetyMetadata, CommandDispatchStatus.ClientNotFound, null, null, cancellationToken)
                    .ConfigureAwait(false);
                return CommandDispatchResult.ClientNotFound(request.CorrelationId, safetyMetadata);
            }

            try
            {
                await session.SendAsync(request.Message, cancellationToken).ConfigureAwait(false);
                await WriteAuditAsync(request, safetyMetadata, CommandDispatchStatus.Sent, null, null, cancellationToken)
                    .ConfigureAwait(false);
                return CommandDispatchResult.Sent(request.CorrelationId, safetyMetadata);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                await WriteAuditAsync(
                    request,
                    safetyMetadata,
                    CommandDispatchStatus.Faulted,
                    null,
                    exception,
                    CancellationToken.None)
                    .ConfigureAwait(false);
                return CommandDispatchResult.Faulted(request.CorrelationId, safetyMetadata, exception);
            }
        }

        private Task WriteAuditAsync(
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata,
            CommandDispatchStatus status,
            string denialReason,
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
                safetyMetadata.SafetyClass.ToString(),
                safetyMetadata.RequiresPermission,
                safetyMetadata.RequiresConsent,
                status.ToString(),
                exception == null ? denialReason : exception.Message);

            return _auditSink.WriteAsync(auditEvent, cancellationToken);
        }
    }
}
