namespace MasterSplinter.Client.Core.Processes
{
    public sealed class ProcessStartResult
    {
        private ProcessStartResult(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public bool IsSuccess { get; }

        public static ProcessStartResult Success()
        {
            return new ProcessStartResult(true);
        }

        public static ProcessStartResult Error()
        {
            return new ProcessStartResult(false);
        }
    }
}
