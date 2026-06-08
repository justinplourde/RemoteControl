using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages.ReverseProxy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.ReverseProxy
{
    public sealed class ReverseProxyDisconnectHandler : IMessageHandler<ReverseProxyDisconnect>
    {
        private readonly IReverseProxyProvider _provider;

        public ReverseProxyDisconnectHandler(IReverseProxyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task HandleAsync(IClientContext context, ReverseProxyDisconnect message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (!(context is IClientCommandContext commandContext))
                throw new InvalidOperationException("Reverse proxy handlers require a command context that can send messages.");

            await _provider.DisconnectAsync(message, commandContext.SendAsync, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
