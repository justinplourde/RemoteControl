using System;

namespace LocationRemote.Client.Core.Dispatch
{
    public sealed class DispatchResult
    {
        private DispatchResult(DispatchStatus status, Exception exception)
        {
            Status = status;
            Exception = exception;
        }

        public DispatchStatus Status { get; }

        public Exception Exception { get; }

        public static DispatchResult Handled()
        {
            return new DispatchResult(DispatchStatus.Handled, null);
        }

        public static DispatchResult Unhandled()
        {
            return new DispatchResult(DispatchStatus.Unhandled, null);
        }

        public static DispatchResult Faulted(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return new DispatchResult(DispatchStatus.Faulted, exception);
        }
    }
}
