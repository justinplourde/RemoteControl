using System;

namespace LocationRemote.Server.Core.Commands
{
    public sealed class CommandDispatchResult
    {
        private CommandDispatchResult(CommandDispatchStatus status, Exception exception)
        {
            Status = status;
            Exception = exception;
        }

        public CommandDispatchStatus Status { get; }

        public Exception Exception { get; }

        public static CommandDispatchResult Sent()
        {
            return new CommandDispatchResult(CommandDispatchStatus.Sent, null);
        }

        public static CommandDispatchResult ClientNotFound()
        {
            return new CommandDispatchResult(CommandDispatchStatus.ClientNotFound, null);
        }

        public static CommandDispatchResult Faulted(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return new CommandDispatchResult(CommandDispatchStatus.Faulted, exception);
        }
    }
}
