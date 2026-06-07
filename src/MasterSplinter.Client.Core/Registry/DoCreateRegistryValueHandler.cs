using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoCreateRegistryValueHandler : IResponseMessageHandler<DoCreateRegistryValue>
    {
        private readonly IRegistryValueMutationProvider _provider;

        public DoCreateRegistryValueHandler(IRegistryValueMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoCreateRegistryValue message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryValueMutationResult result = _provider.CreateValue(message.KeyPath, message.Kind);
            return Task.FromResult<IMessage>(new GetCreateRegistryValueResponse
            {
                KeyPath = message.KeyPath,
                Value = result.Value,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
