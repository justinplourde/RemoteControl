using MasterSplinter.Common.Enums;

namespace MasterSplinter.Client.Core.FileSystem
{
    public interface IPathDeleteProvider
    {
        PathDeleteResult Delete(string path, FileType pathType);
    }
}
