using Quasar.Common.Enums;
using System;
using System.IO;
using System.Security;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class PathDeleteProvider : IPathDeleteProvider
    {
        public PathDeleteResult Delete(string path, FileType pathType)
        {
            if (string.IsNullOrWhiteSpace(path))
                return PathDeleteResult.Error("DeletePath Path is required");

            try
            {
                if (pathType == FileType.File)
                {
                    if (!File.Exists(path))
                        return PathDeleteResult.Error("DeletePath Path not found");

                    File.Delete(path);
                    return PathDeleteResult.Success("Deleted file");
                }

                if (pathType == FileType.Directory)
                    return PathDeleteResult.Error("DeletePath Directory delete requires explicit recursive policy");

                return PathDeleteResult.Error("DeletePath Unsupported path type");
            }
            catch (UnauthorizedAccessException)
            {
                return PathDeleteResult.Error("DeletePath No permission");
            }
            catch (SecurityException)
            {
                return PathDeleteResult.Error("DeletePath No permission");
            }
            catch (PathTooLongException)
            {
                return PathDeleteResult.Error("DeletePath Path too long");
            }
            catch (DirectoryNotFoundException)
            {
                return PathDeleteResult.Error("DeletePath Path not found");
            }
            catch (IOException)
            {
                return PathDeleteResult.Error("DeletePath I/O error");
            }
            catch (Exception)
            {
                return PathDeleteResult.Error("DeletePath Failed");
            }
        }
    }
}
