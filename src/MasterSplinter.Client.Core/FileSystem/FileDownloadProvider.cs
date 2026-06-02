using System;
using System.IO;
using System.Security;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class FileDownloadProvider : IFileDownloadProvider
    {
        public const long DefaultMaxFileSizeBytes = 100L * 1024L * 1024L;

        private readonly long _maxFileSizeBytes;

        public FileDownloadProvider()
            : this(DefaultMaxFileSizeBytes)
        {
        }

        public FileDownloadProvider(long maxFileSizeBytes)
        {
            if (maxFileSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFileSizeBytes));

            _maxFileSizeBytes = maxFileSizeBytes;
        }

        public FileDownloadResult OpenRead(string remotePath)
        {
            if (string.IsNullOrWhiteSpace(remotePath))
                return FileDownloadResult.Error("FileTransferRequest Path is required");

            try
            {
                var fileInfo = new FileInfo(remotePath);
                if (!fileInfo.Exists)
                    return FileDownloadResult.Error("FileTransferRequest File not found");
                if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    return FileDownloadResult.Error("FileTransferRequest Path is a directory");
                if (fileInfo.Length > _maxFileSizeBytes)
                    return FileDownloadResult.Error("FileTransferRequest File exceeds size limit");

                Stream stream = File.Open(remotePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return FileDownloadResult.Success(remotePath, fileInfo.Length, stream);
            }
            catch (UnauthorizedAccessException)
            {
                return FileDownloadResult.Error("FileTransferRequest No permission");
            }
            catch (SecurityException)
            {
                return FileDownloadResult.Error("FileTransferRequest No permission");
            }
            catch (PathTooLongException)
            {
                return FileDownloadResult.Error("FileTransferRequest Path too long");
            }
            catch (DirectoryNotFoundException)
            {
                return FileDownloadResult.Error("FileTransferRequest Directory not found");
            }
            catch (FileNotFoundException)
            {
                return FileDownloadResult.Error("FileTransferRequest File not found");
            }
            catch (IOException)
            {
                return FileDownloadResult.Error("FileTransferRequest I/O error");
            }
            catch (Exception)
            {
                return FileDownloadResult.Error("FileTransferRequest Failed");
            }
        }
    }
}
