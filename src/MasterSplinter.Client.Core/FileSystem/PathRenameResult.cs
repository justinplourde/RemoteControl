namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class PathRenameResult
    {
        private PathRenameResult(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public bool IsSuccess { get; }
        public string Message { get; }

        public static PathRenameResult Success(string message)
        {
            return new PathRenameResult(true, message);
        }

        public static PathRenameResult Error(string message)
        {
            return new PathRenameResult(false, message);
        }
    }
}
