namespace MasterSplinter.Client.Core.Processes
{
    public sealed class ProcessEndResult
    {
        private ProcessEndResult(bool isSuccess)
        {
            IsSuccess = isSuccess;
        }

        public bool IsSuccess { get; }

        public static ProcessEndResult Success() => new ProcessEndResult(true);

        public static ProcessEndResult Error() => new ProcessEndResult(false);
    }
}
