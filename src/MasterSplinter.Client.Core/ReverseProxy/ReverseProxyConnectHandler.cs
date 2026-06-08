using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Messages.ReverseProxy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.ReverseProxy
{
    public sealed class ReverseProxyConnectHandler : IMessageHandler<ReverseProxyConnect>
    {
        private readonly IReverseProxyProvider _provider;

        public ReverseProxyConnectHandler(IReverseProxyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task HandleAsync(IClientContext context, ReverseProxyConnect message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!(context is IClientCommandContext commandContext))
                throw new InvalidOperationException("Reverse proxy handlers require a command context that can send messages.");

            await _provider.ConnectAsync(message, commandContext.SendAsync, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
