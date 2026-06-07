using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class FileUploadProvider : IFileUploadProvider
    {
        public const long DefaultMaxFileSizeBytes = 100L * 1024L * 1024L;

        private readonly object _gate = new object();
        private readonly Dictionary<int, UploadSession> _sessions = new Dictionary<int, UploadSession>();
        private readonly long _maxFileSizeBytes;

        public FileUploadProvider()
            : this(DefaultMaxFileSizeBytes)
        {
        }

        public FileUploadProvider(long maxFileSizeBytes)
        {
            if (maxFileSizeBytes <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxFileSizeBytes));

            _maxFileSizeBytes = maxFileSizeBytes;
        }

        public FileUploadResult WriteChunk(FileTransferChunk message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (message.Chunk == null || message.Chunk.Data == null)
                return FileUploadResult.Error("FileTransferChunk Data is required");
            if (string.IsNullOrWhiteSpace(message.FilePath))
                return FileUploadResult.Error("FileTransferChunk FilePath is required");
            if (message.FileSize < 0 || message.FileSize > _maxFileSizeBytes)
                return FileUploadResult.Error("FileTransferChunk File exceeds size limit");

            lock (_gate)
            {
                try
                {
                    UploadSession session = GetOrCreateSession(message);
                    if (message.Chunk.Offset != session.BytesWritten)
                        return CancelWithError(message.Id, "FileTransferChunk Offset mismatch");
                    if (session.BytesWritten + message.Chunk.Data.Length > message.FileSize)
                        return CancelWithError(message.Id, "FileTransferChunk Exceeds declared size");

                    session.Stream.Write(message.Chunk.Data, 0, message.Chunk.Data.Length);
                    session.BytesWritten += message.Chunk.Data.Length;

                    if (session.BytesWritten < message.FileSize)
                        return FileUploadResult.Pending();

                    string finalPath = session.FinalPath;
                    CompleteAndRemove(message.Id, moveTempFile: true);
                    return FileUploadResult.Complete(finalPath);
                }
                catch (UnauthorizedAccessException)
                {
                    return CancelWithError(message.Id, "FileTransferChunk No permission");
                }
                catch (SecurityException)
                {
                    return CancelWithError(message.Id, "FileTransferChunk No permission");
                }
                catch (PathTooLongException)
                {
                    return CancelWithError(message.Id, "FileTransferChunk Path too long");
                }
                catch (DirectoryNotFoundException)
                {
                    return CancelWithError(message.Id, "FileTransferChunk Directory not found");
                }
                catch (IOException)
                {
                    return CancelWithError(message.Id, "FileTransferChunk I/O error");
                }
                catch (Exception)
                {
                    return CancelWithError(message.Id, "FileTransferChunk Failed");
                }
            }
        }

        public void Cancel(int id)
        {
            lock (_gate)
            {
                DisposeAndRemove(id);
            }
        }

        private UploadSession GetOrCreateSession(FileTransferChunk message)
        {
            if (_sessions.TryGetValue(message.Id, out UploadSession existing))
                return existing;

            if (message.Chunk.Offset != 0)
                throw new IOException("First upload chunk must start at offset zero.");

            string finalPath = Path.GetFullPath(message.FilePath);
            if (Directory.Exists(finalPath))
                throw new IOException("Upload target is a directory.");
            if (File.Exists(finalPath))
                throw new IOException("Upload target already exists.");

            string directory = Path.GetDirectoryName(finalPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string tempPath = finalPath + $".uploading-{message.Id}";
            Stream stream = File.Open(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            var session = new UploadSession(finalPath, tempPath, stream);
            _sessions.Add(message.Id, session);
            return session;
        }

        private FileUploadResult CancelWithError(int id, string errorMessage)
        {
            DisposeAndRemove(id);
            return FileUploadResult.Error(errorMessage);
        }

        private void DisposeAndRemove(int id)
        {
            if (!_sessions.TryGetValue(id, out UploadSession session))
                return;

            _sessions.Remove(id);
            session.Stream.Dispose();
            if (File.Exists(session.TempPath))
                File.Delete(session.TempPath);
        }

        private void CompleteAndRemove(int id, bool moveTempFile)
        {
            if (!_sessions.TryGetValue(id, out UploadSession session))
                return;

            _sessions.Remove(id);
            session.Stream.Dispose();
            if (!moveTempFile)
                return;

            try
            {
                File.Move(session.TempPath, session.FinalPath);
            }
            catch
            {
                if (File.Exists(session.TempPath))
                    File.Delete(session.TempPath);
                throw;
            }
        }

        private sealed class UploadSession
        {
            public UploadSession(string finalPath, string tempPath, Stream stream)
            {
                FinalPath = finalPath;
                TempPath = tempPath;
                Stream = stream;
            }

            public string FinalPath { get; }
            public string TempPath { get; }
            public Stream Stream { get; }
            public long BytesWritten { get; set; }
        }
    }
}
