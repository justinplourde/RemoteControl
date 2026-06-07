using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class DoPathDeleteHandler : IResponseMessageHandler<DoPathDelete>
    {
        private readonly IPathDeleteProvider _provider;

        public DoPathDeleteHandler(IPathDeleteProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoPathDelete message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            PathDeleteResult result = _provider.Delete(message.Path, message.PathType);
            return Task.FromResult<IMessage>(new SetStatusFileManager
            {
                Message = result.Message,
                SetLastDirectorySeen = false
            });
        }
    }
}
