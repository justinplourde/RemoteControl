using Quasar.Common.Enums;
using System;
using System.IO;
using System.Security;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class PathRenameProvider : IPathRenameProvider
    {
        public PathRenameResult Rename(string path, string newPath, FileType pathType)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(newPath))
                return PathRenameResult.Error("RenamePath Path is required");

            try
            {
                if (pathType == FileType.Directory)
                {
                    if (!Directory.Exists(path))
                        return PathRenameResult.Error("RenamePath Path not found");
                    if (Directory.Exists(newPath) || File.Exists(newPath))
                        return PathRenameResult.Error("RenamePath Target already exists");

                    Directory.Move(path, newPath);
                    return PathRenameResult.Success("Renamed directory");
                }

                if (pathType == FileType.File)
                {
                    if (!File.Exists(path))
                        return PathRenameResult.Error("RenamePath Path not found");
                    if (Directory.Exists(newPath) || File.Exists(newPath))
                        return PathRenameResult.Error("RenamePath Target already exists");

                    File.Move(path, newPath);
                    return PathRenameResult.Success("Renamed file");
                }

                return PathRenameResult.Error("RenamePath Unsupported path type");
            }
            catch (UnauthorizedAccessException)
            {
                return PathRenameResult.Error("RenamePath No permission");
            }
            catch (SecurityException)
            {
                return PathRenameResult.Error("RenamePath No permission");
            }
            catch (PathTooLongException)
            {
                return PathRenameResult.Error("RenamePath Path too long");
            }
            catch (DirectoryNotFoundException)
            {
                return PathRenameResult.Error("RenamePath Path not found");
            }
            catch (IOException)
            {
                return PathRenameResult.Error("RenamePath I/O error");
            }
            catch (Exception)
            {
                return PathRenameResult.Error("RenamePath Failed");
            }
        }
    }
}
