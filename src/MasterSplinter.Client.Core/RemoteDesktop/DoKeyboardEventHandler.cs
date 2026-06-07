using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public sealed class DoKeyboardEventHandler : IResponseMessageHandler<DoKeyboardEvent>
    {
        private readonly IRemoteInputProvider _provider;

        public DoKeyboardEventHandler(IRemoteInputProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoKeyboardEvent message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            RemoteInputResult result = _provider.SendKeyboardEvent(message);
            return Task.FromResult<IMessage>(new SetStatus
            {
                Message = result.IsSuccess ? "Keyboard event sent." : $"Keyboard event failed: {result.ErrorMessage}"
            });
        }
    }
}
