using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoClientReconnectHandler : IMessageHandler<DoClientReconnect>
    {
        public async Task HandleAsync(IClientContext context, DoClientReconnect message, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!(context is IClientLifecycleContext lifecycleContext))
                throw new InvalidOperationException("Client reconnect requires a lifecycle-capable command context.");

            await lifecycleContext.SendAsync(new SetStatus { Message = "Client reconnect requested." }, cancellationToken)
                .ConfigureAwait(false);
            await lifecycleContext.RequestReconnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
