using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandDispatchResult
    {
        private CommandDispatchResult(
            Guid correlationId,
            CommandDispatchStatus status,
            CommandSafetyMetadata safetyMetadata,
            string denialReason,
            Exception exception)
        {
            CorrelationId = correlationId;
            Status = status;
            SafetyMetadata = safetyMetadata;
            DenialReason = denialReason;
            Exception = exception;
        }

        public Guid CorrelationId { get; }

        public CommandDispatchStatus Status { get; }

        public CommandSafetyMetadata SafetyMetadata { get; }

        public string DenialReason { get; }

        public Exception Exception { get; }

        public static CommandDispatchResult Sent(Guid correlationId, CommandSafetyMetadata safetyMetadata)
        {
            return new CommandDispatchResult(correlationId, CommandDispatchStatus.Sent, safetyMetadata, null, null);
        }

        public static CommandDispatchResult ClientNotFound(Guid correlationId, CommandSafetyMetadata safetyMetadata)
        {
            return new CommandDispatchResult(
                correlationId,
                CommandDispatchStatus.ClientNotFound,
                safetyMetadata,
                null,
                null);
        }

        public static CommandDispatchResult Denied(
            Guid correlationId,
            CommandDispatchStatus status,
            CommandSafetyMetadata safetyMetadata,
            string reason)
        {
            if (status != CommandDispatchStatus.PermissionDenied &&
                status != CommandDispatchStatus.ConsentRequired)
            {
                throw new ArgumentException("Denied dispatch results require a denial status.", nameof(status));
            }

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Denial reason is required.", nameof(reason));

            return new CommandDispatchResult(correlationId, status, safetyMetadata, reason, null);
        }

        public static CommandDispatchResult Faulted(
            Guid correlationId,
            CommandSafetyMetadata safetyMetadata,
            Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return new CommandDispatchResult(
                correlationId,
                CommandDispatchStatus.Faulted,
                safetyMetadata,
                null,
                exception);
        }
    }
}
