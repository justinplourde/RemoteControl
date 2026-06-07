using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoClientUninstallHandler : IMessageHandler<DoClientUninstall>
    {
        private readonly IClientUninstallProvider _provider;

        public DoClientUninstallHandler(IClientUninstallProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task HandleAsync(IClientContext context, DoClientUninstall message, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (!(context is IClientLifecycleContext lifecycleContext))
                throw new InvalidOperationException("Client uninstall requires a lifecycle-capable command context.");

            await lifecycleContext.SendAsync(new SetStatus { Message = "Uninstalling... good bye :-(" }, cancellationToken)
                .ConfigureAwait(false);

            ClientUninstallResult result = _provider.Uninstall();
            if (!result.IsSuccess)
            {
                await lifecycleContext.SendAsync(new SetStatus { Message = $"Uninstall failed: {result.ErrorMessage}" }, cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await lifecycleContext.RequestDisconnectAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
