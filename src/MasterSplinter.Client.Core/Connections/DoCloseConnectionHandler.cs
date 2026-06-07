using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Connections
{
    public sealed class DoCloseConnectionHandler : IResponseMessageHandler<DoCloseConnection>
    {
        private readonly IConnectionCloseProvider _provider;

        public DoCloseConnectionHandler(IConnectionCloseProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoCloseConnection message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            TcpConnectionCloseResult result = _provider.CloseConnection(
                message.LocalAddress,
                message.LocalPort,
                message.RemoteAddress,
                message.RemotePort);

            return Task.FromResult<IMessage>(new GetConnectionsResponse
            {
                Connections = result.Connections
            });
        }
    }
}
