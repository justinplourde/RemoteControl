namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class FileUploadResult
    {
        private FileUploadResult(bool isComplete, string completedPath, string errorMessage)
        {
            IsComplete = isComplete;
            CompletedPath = completedPath;
            ErrorMessage = errorMessage;
        }

        public bool IsComplete { get; }
        public string CompletedPath { get; }
        public string ErrorMessage { get; }
        public bool IsError => !string.IsNullOrWhiteSpace(ErrorMessage);

        public static FileUploadResult Pending()
        {
            return new FileUploadResult(false, null, null);
        }

        public static FileUploadResult Complete(string completedPath)
        {
            return new FileUploadResult(true, completedPath, null);
        }

        public static FileUploadResult Error(string errorMessage)
        {
            return new FileUploadResult(false, null, errorMessage);
        }
    }
}
