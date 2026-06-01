using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandDispatchResult
    {
        private CommandDispatchResult(
            Guid correlationId,
            CommandDispatchStatus status,
            CommandSafetyMetadata safetyMetadata,
            Exception exception)
        {
            CorrelationId = correlationId;
            Status = status;
            SafetyMetadata = safetyMetadata;
            Exception = exception;
        }

        public Guid CorrelationId { get; }

        public CommandDispatchStatus Status { get; }

        public CommandSafetyMetadata SafetyMetadata { get; }

        public Exception Exception { get; }

        public static CommandDispatchResult Sent(Guid correlationId, CommandSafetyMetadata safetyMetadata)
        {
            return new CommandDispatchResult(correlationId, CommandDispatchStatus.Sent, safetyMetadata, null);
        }

        public static CommandDispatchResult ClientNotFound(Guid correlationId, CommandSafetyMetadata safetyMetadata)
        {
            return new CommandDispatchResult(correlationId, CommandDispatchStatus.ClientNotFound, safetyMetadata, null);
        }

        public static CommandDispatchResult Faulted(
            Guid correlationId,
            CommandSafetyMetadata safetyMetadata,
            Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return new CommandDispatchResult(correlationId, CommandDispatchStatus.Faulted, safetyMetadata, exception);
        }
    }
}
