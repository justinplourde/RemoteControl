using MasterSplinter.Common.Models;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class DirectoryListResult
    {
        private DirectoryListResult(string remotePath, FileSystemEntry[] items, string errorMessage)
        {
            RemotePath = remotePath;
            Items = items;
            ErrorMessage = errorMessage;
        }

        public string RemotePath { get; }

        public FileSystemEntry[] Items { get; }

        public string ErrorMessage { get; }

        public bool IsSuccess => ErrorMessage == null;

        public static DirectoryListResult Success(string remotePath, FileSystemEntry[] items)
        {
            return new DirectoryListResult(remotePath, items, null);
        }

        public static DirectoryListResult Error(string errorMessage)
        {
            return new DirectoryListResult(null, null, errorMessage);
        }
    }
}
