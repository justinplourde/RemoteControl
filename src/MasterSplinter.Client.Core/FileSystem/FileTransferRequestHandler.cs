using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class FileTransferRequestHandler : IMessageHandler<FileTransferRequest>
    {
        public const int DefaultChunkSize = 64 * 1024;

        private readonly IFileDownloadProvider _provider;
        private readonly int _chunkSize;

        public FileTransferRequestHandler(IFileDownloadProvider provider)
            : this(provider, DefaultChunkSize)
        {
        }

        public FileTransferRequestHandler(IFileDownloadProvider provider, int chunkSize)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            if (chunkSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(chunkSize));

            _chunkSize = chunkSize;
        }

        public async Task HandleAsync(IClientContext context, FileTransferRequest message, CancellationToken cancellationToken)
        {
            if (!(context is IClientCommandContext commandContext))
                throw new InvalidOperationException("File transfer handlers require a command context that can send messages.");
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            FileDownloadResult result = _provider.OpenRead(message.RemotePath);
            if (!result.IsSuccess)
            {
                await commandContext.SendAsync(new FileTransferCancel
                {
                    Id = message.Id,
                    Reason = result.ErrorMessage
                }, cancellationToken).ConfigureAwait(false);
                return;
            }

            using (Stream stream = result.Stream)
            {
                var buffer = new byte[_chunkSize];
                long offset = 0;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                        break;

                    var data = new byte[read];
                    Buffer.BlockCopy(buffer, 0, data, 0, read);

                    await commandContext.SendAsync(new FileTransferChunk
                    {
                        Id = message.Id,
                        FilePath = result.RemotePath,
                        FileSize = result.FileSize,
                        Chunk = new FileChunk
                        {
                            Offset = offset,
                            Data = data
                        }
                    }, cancellationToken).ConfigureAwait(false);

                    offset += read;
                }
            }

            await commandContext.SendAsync(new FileTransferComplete
            {
                Id = message.Id,
                FilePath = result.RemotePath
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
