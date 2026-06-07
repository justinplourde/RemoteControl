using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoClientDisconnectHandler : IMessageHandler<DoClientDisconnect>
    {
        public async Task HandleAsync(IClientContext context, DoClientDisconnect message, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            if (!(context is IClientLifecycleContext lifecycleContext))
                throw new InvalidOperationException("Client disconnect requires a lifecycle-capable command context.");

            await lifecycleContext.SendAsync(new SetStatus { Message = "Client disconnect requested." }, cancellationToken)
                .ConfigureAwait(false);
            await lifecycleContext.RequestDisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
