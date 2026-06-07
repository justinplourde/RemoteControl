using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class FileTransferUploadHandler :
        IMessageHandler<FileTransferChunk>,
        IMessageHandler<FileTransferCancel>
    {
        private readonly IFileUploadProvider _provider;

        public FileTransferUploadHandler(IFileUploadProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task HandleAsync(IClientContext context, FileTransferChunk message, CancellationToken cancellationToken)
        {
            if (!(context is IClientCommandContext commandContext))
                throw new InvalidOperationException("File transfer upload handlers require a command context that can send messages.");
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            FileUploadResult result = _provider.WriteChunk(message);
            if (result.IsError)
            {
                await commandContext.SendAsync(new FileTransferCancel
                {
                    Id = message.Id,
                    Reason = result.ErrorMessage
                }, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (result.IsComplete)
            {
                await commandContext.SendAsync(new FileTransferComplete
                {
                    Id = message.Id,
                    FilePath = result.CompletedPath
                }, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task HandleAsync(IClientContext context, FileTransferCancel message, CancellationToken cancellationToken)
        {
            if (!(context is IClientCommandContext commandContext))
                throw new InvalidOperationException("File transfer upload handlers require a command context that can send messages.");
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            _provider.Cancel(message.Id);
            await commandContext.SendAsync(new FileTransferCancel
            {
                Id = message.Id,
                Reason = message.Reason
            }, cancellationToken).ConfigureAwait(false);
        }
    }
}
