using Quasar.Common.Enums;

namespace MasterSplinter.Client.Core.FileSystem
{
    public interface IPathRenameProvider
    {
        PathRenameResult Rename(string path, string newPath, FileType pathType);
    }
}
