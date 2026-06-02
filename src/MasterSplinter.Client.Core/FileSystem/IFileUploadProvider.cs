using Quasar.Common.Messages;

namespace MasterSplinter.Client.Core.FileSystem
{
    public interface IFileUploadProvider
    {
        FileUploadResult WriteChunk(FileTransferChunk message);

        void Cancel(int id);
    }
}
