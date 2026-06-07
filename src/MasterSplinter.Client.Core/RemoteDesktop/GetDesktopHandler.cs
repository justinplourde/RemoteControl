using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public sealed class GetDesktopHandler : IResponseMessageHandler<GetDesktop>
    {
        private readonly IDesktopCaptureProvider _provider;

        public GetDesktopHandler(IDesktopCaptureProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetDesktop message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IMessage>(_provider.Capture(message));
        }
    }
}
