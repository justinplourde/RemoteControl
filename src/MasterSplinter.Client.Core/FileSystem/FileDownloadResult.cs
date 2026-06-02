using System;
using System.IO;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class FileDownloadResult
    {
        private FileDownloadResult(bool isSuccess, string remotePath, long fileSize, Stream stream, string errorMessage)
        {
            IsSuccess = isSuccess;
            RemotePath = remotePath;
            FileSize = fileSize;
            Stream = stream;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }
        public string RemotePath { get; }
        public long FileSize { get; }
        public Stream Stream { get; }
        public string ErrorMessage { get; }

        public static FileDownloadResult Success(string remotePath, long fileSize, Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            return new FileDownloadResult(true, remotePath, fileSize, stream, null);
        }

        public static FileDownloadResult Error(string errorMessage)
        {
            return new FileDownloadResult(false, null, 0, null, errorMessage);
        }
    }
}
