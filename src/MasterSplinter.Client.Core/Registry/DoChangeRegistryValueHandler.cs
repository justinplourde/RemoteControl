using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoChangeRegistryValueHandler : IResponseMessageHandler<DoChangeRegistryValue>
    {
        private readonly IRegistryValueMutationProvider _provider;

        public DoChangeRegistryValueHandler(IRegistryValueMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoChangeRegistryValue message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryValueMutationResult result = _provider.ChangeValue(message.KeyPath, message.Value);
            return Task.FromResult<IMessage>(new GetChangeRegistryValueResponse
            {
                KeyPath = message.KeyPath,
                Value = message.Value,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
