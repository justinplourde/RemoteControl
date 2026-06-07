using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class DoCreateRegistryKeyHandler : IResponseMessageHandler<DoCreateRegistryKey>
    {
        private readonly IRegistryKeyMutationProvider _provider;

        public DoCreateRegistryKeyHandler(IRegistryKeyMutationProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoCreateRegistryKey message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            RegistryKeyMutationResult result = _provider.CreateKey(message.ParentPath);
            return Task.FromResult<IMessage>(new GetCreateRegistryKeyResponse
            {
                ParentPath = message.ParentPath,
                Match = result.Match,
                IsError = !result.IsSuccess,
                ErrorMsg = result.ErrorMessage
            });
        }
    }
}
