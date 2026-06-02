using System.IO;

namespace MasterSplinter.Client.Core.FileSystem
{
    public interface IFileDownloadProvider
    {
        FileDownloadResult OpenRead(string remotePath);
    }
}
