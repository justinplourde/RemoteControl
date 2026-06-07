using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class DoPathRenameHandler : IResponseMessageHandler<DoPathRename>
    {
        private readonly IPathRenameProvider _provider;

        public DoPathRenameHandler(IPathRenameProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoPathRename message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            PathRenameResult result = _provider.Rename(message.Path, message.NewPath, message.PathType);
            return Task.FromResult<IMessage>(new SetStatusFileManager
            {
                Message = result.Message,
                SetLastDirectorySeen = false
            });
        }
    }
}
