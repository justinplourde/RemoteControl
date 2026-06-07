using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Models;
using System;
using System.IO;
using System.Security;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class DirectoryProvider : IDirectoryProvider
    {
        public DirectoryListResult GetDirectory(string remotePath)
        {
            try
            {
                var directoryInfo = new DirectoryInfo(remotePath);
                FileInfo[] files = directoryInfo.GetFiles();
                DirectoryInfo[] directories = directoryInfo.GetDirectories();
                var items = new FileSystemEntry[files.Length + directories.Length];

                int offset = 0;
                for (int index = 0; index < directories.Length; index++, offset++)
                {
                    items[index] = new FileSystemEntry
                    {
                        EntryType = FileType.Directory,
                        Name = directories[index].Name,
                        Size = 0,
                        LastAccessTimeUtc = directories[index].LastAccessTimeUtc
                    };
                }

                for (int index = 0; index < files.Length; index++)
                {
                    FileInfo file = files[index];
                    items[index + offset] = new FileSystemEntry
                    {
                        EntryType = FileType.File,
                        Name = file.Name,
                        Size = file.Length,
                        ContentType = ToContentType(Path.GetExtension(file.Name)),
                        LastAccessTimeUtc = file.LastAccessTimeUtc
                    };
                }

                return DirectoryListResult.Success(remotePath, items);
            }
            catch (UnauthorizedAccessException)
            {
                return DirectoryListResult.Error("GetDirectory No permission");
            }
            catch (SecurityException)
            {
                return DirectoryListResult.Error("GetDirectory No permission");
            }
            catch (PathTooLongException)
            {
                return DirectoryListResult.Error("GetDirectory Path too long");
            }
            catch (DirectoryNotFoundException)
            {
                return DirectoryListResult.Error("GetDirectory Directory not found");
            }
            catch (FileNotFoundException)
            {
                return DirectoryListResult.Error("GetDirectory File not found");
            }
            catch (IOException)
            {
                return DirectoryListResult.Error("GetDirectory I/O error");
            }
            catch (Exception)
            {
                return DirectoryListResult.Error("GetDirectory Failed");
            }
        }

        private static ContentType ToContentType(string fileExtension)
        {
            switch ((fileExtension ?? string.Empty).ToLowerInvariant())
            {
                case ".exe":
                    return ContentType.Application;
                case ".txt":
                case ".log":
                case ".conf":
                case ".cfg":
                case ".asc":
                    return ContentType.Text;
                case ".rar":
                case ".zip":
                case ".zipx":
                case ".tar":
                case ".tgz":
                case ".gz":
                case ".s7z":
                case ".7z":
                case ".bz2":
                case ".cab":
                case ".zz":
                case ".apk":
                    return ContentType.Archive;
                case ".doc":
                case ".docx":
                case ".odt":
                    return ContentType.Word;
                case ".pdf":
                    return ContentType.Pdf;
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                case ".ico":
                    return ContentType.Image;
                case ".mp4":
                case ".mov":
                case ".avi":
                case ".wmv":
                case ".mkv":
                case ".m4v":
                case ".flv":
                    return ContentType.Video;
                case ".mp3":
                case ".wav":
                case ".pls":
                case ".m3u":
                case ".m4a":
                    return ContentType.Audio;
                default:
                    return ContentType.Blob;
            }
        }
    }
}
