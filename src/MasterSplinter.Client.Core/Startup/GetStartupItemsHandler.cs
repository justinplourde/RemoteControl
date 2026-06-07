using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Startup
{
    public sealed class GetStartupItemsHandler : IResponseMessageHandler<GetStartupItems>
    {
        private readonly IStartupItemProvider _provider;

        public GetStartupItemsHandler(IStartupItemProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetStartupItems message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            StartupItemsResult result = _provider.GetStartupItems();
            if (!result.IsSuccess)
            {
                return Task.FromResult<IMessage>(new SetStatus
                {
                    Message = result.ErrorMessage
                });
            }

            return Task.FromResult<IMessage>(new GetStartupItemsResponse
            {
                StartupItems = result.StartupItems
            });
        }
    }
}
