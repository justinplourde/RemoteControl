namespace MasterSplinter.Client.Core.FileSystem
{
    public interface IDirectoryProvider
    {
        DirectoryListResult GetDirectory(string remotePath);
    }
}
