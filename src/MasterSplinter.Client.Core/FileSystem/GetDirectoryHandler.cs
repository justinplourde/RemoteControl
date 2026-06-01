using MasterSplinter.Client.Core.Dispatch;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class GetDirectoryHandler : IResponseMessageHandler<GetDirectory>
    {
        private readonly IDirectoryProvider _provider;

        public GetDirectoryHandler(IDirectoryProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetDirectory message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            DirectoryListResult result = _provider.GetDirectory(message.RemotePath);
            if (!result.IsSuccess)
            {
                return Task.FromResult<IMessage>(new SetStatusFileManager
                {
                    Message = result.ErrorMessage,
                    SetLastDirectorySeen = true
                });
            }

            return Task.FromResult<IMessage>(new GetDirectoryResponse
            {
                RemotePath = result.RemotePath,
                Items = result.Items
            });
        }
    }
}
