using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoRenameRegistryKeyHandler : IResponseMessageHandler<DoRenameRegistryKey>
    {
        private readonly IRegistryKeyMutationProvider _provider;

        public DoRenameRegistryKeyHandler(IRegistryKeyMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoRenameRegistryKey message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryKeyMutationResult result = _provider.RenameKey(message.ParentPath, message.OldKeyName, message.NewKeyName);
            return Task.FromResult<IMessage>(new GetRenameRegistryKeyResponse
            {
                ParentPath = message.ParentPath,
                OldKeyName = message.OldKeyName,
                NewKeyName = message.NewKeyName,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
