using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Connections
{
    public sealed class GetConnectionsHandler : IResponseMessageHandler<GetConnections>
    {
        private readonly IConnectionProvider _provider;

        public GetConnectionsHandler(IConnectionProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetConnections message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IMessage>(new GetConnectionsResponse
            {
                Connections = _provider.GetConnections()
            });
        }
    }
}
