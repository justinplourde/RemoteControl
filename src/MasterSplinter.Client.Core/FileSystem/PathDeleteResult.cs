namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class PathDeleteResult
    {
        private PathDeleteResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public bool IsSuccess { get; }
        public string Message { get; }

        public static PathDeleteResult Success(string message) => new PathDeleteResult(true, message);

        public static PathDeleteResult Error(string message) => new PathDeleteResult(false, message);
    }
}
