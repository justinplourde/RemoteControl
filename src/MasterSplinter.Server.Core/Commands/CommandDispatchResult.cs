using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandDispatchResult
    {
        private CommandDispatchResult(Guid correlationId, CommandDispatchStatus status, Exception exception)
        {
            CorrelationId = correlationId;
            Status = status;
            Exception = exception;
        }

        public Guid CorrelationId { get; }

        public CommandDispatchStatus Status { get; }

        public Exception Exception { get; }

        public static CommandDispatchResult Sent(Guid correlationId)
        {
            return new CommandDispatchResult(correlationId, CommandDispatchStatus.Sent, null);
        }

        public static CommandDispatchResult ClientNotFound(Guid correlationId)
        {
            return new CommandDispatchResult(correlationId, CommandDispatchStatus.ClientNotFound, null);
        }

        public static CommandDispatchResult Faulted(Guid correlationId, Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return new CommandDispatchResult(correlationId, CommandDispatchStatus.Faulted, exception);
        }
    }
}
