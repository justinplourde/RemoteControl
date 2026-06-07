using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoRenameRegistryValueHandler : IResponseMessageHandler<DoRenameRegistryValue>
    {
        private readonly IRegistryValueMutationProvider _provider;

        public DoRenameRegistryValueHandler(IRegistryValueMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoRenameRegistryValue message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryValueMutationResult result = _provider.RenameValue(
                message.KeyPath,
                message.OldValueName,
                message.NewValueName);
            return Task.FromResult<IMessage>(new GetRenameRegistryValueResponse
            {
                KeyPath = message.KeyPath,
                OldValueName = message.OldValueName,
                NewValueName = message.NewValueName,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
