using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Startup
{
    public sealed class DoStartupItemRemoveHandler : IResponseMessageHandler<DoStartupItemRemove>
    {
        private readonly IStartupItemMutationProvider _provider;

        public DoStartupItemRemoveHandler(IStartupItemMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoStartupItemRemove message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            StartupItemMutationResult result = _provider.RemoveStartupItem(message.StartupItem);
            return Task.FromResult<IMessage>(new SetStatus
            {
                Message = result.IsSuccess ? "Removed Autostart Item" : result.ErrorMessage
            });
        }
    }
}
