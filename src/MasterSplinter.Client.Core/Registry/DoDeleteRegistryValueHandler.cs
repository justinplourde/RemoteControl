using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoDeleteRegistryValueHandler : IResponseMessageHandler<DoDeleteRegistryValue>
    {
        private readonly IRegistryValueMutationProvider _provider;

        public DoDeleteRegistryValueHandler(IRegistryValueMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoDeleteRegistryValue message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryValueMutationResult result = _provider.DeleteValue(message.KeyPath, message.ValueName);
            return Task.FromResult<IMessage>(new GetDeleteRegistryValueResponse
            {
                KeyPath = message.KeyPath,
                ValueName = message.ValueName,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
