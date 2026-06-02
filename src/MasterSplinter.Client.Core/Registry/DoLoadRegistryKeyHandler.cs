using MasterSplinter.Client.Core.Dispatch;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoLoadRegistryKeyHandler : IResponseMessageHandler<DoLoadRegistryKey>
    {
        private readonly IRegistryKeyProvider _provider;

        public DoLoadRegistryKeyHandler(IRegistryKeyProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoLoadRegistryKey message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            RegistryKeyLoadResult result = _provider.LoadKey(message.RootKeyName);
            return Task.FromResult<IMessage>(new GetRegistryKeysResponse
            {
                RootKey = message.RootKeyName,
                Matches = result.Matches,
                IsError = result.IsError,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
