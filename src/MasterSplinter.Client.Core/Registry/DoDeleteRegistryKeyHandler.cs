using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoDeleteRegistryKeyHandler : IResponseMessageHandler<DoDeleteRegistryKey>
    {
        private readonly IRegistryKeyMutationProvider _provider;

        public DoDeleteRegistryKeyHandler(IRegistryKeyMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoDeleteRegistryKey message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryKeyMutationResult result = _provider.DeleteKey(message.ParentPath, message.KeyName);
            return Task.FromResult<IMessage>(new GetDeleteRegistryKeyResponse
            {
                ParentPath = message.ParentPath,
                KeyName = message.KeyName,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
