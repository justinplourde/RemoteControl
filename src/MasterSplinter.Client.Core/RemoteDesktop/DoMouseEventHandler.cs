using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public sealed class DoMouseEventHandler : IResponseMessageHandler<DoMouseEvent>
    {
        private readonly IRemoteInputProvider _provider;

        public DoMouseEventHandler(IRemoteInputProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoMouseEvent message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            RemoteInputResult result = _provider.SendMouseEvent(message);
            return Task.FromResult<IMessage>(new SetStatus
            {
                Message = result.IsSuccess ? "Mouse event sent." : $"Mouse event failed: {result.ErrorMessage}"
            });
        }
    }
}
