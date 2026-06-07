using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public sealed class GetMonitorsHandler : IResponseMessageHandler<GetMonitors>
    {
        private readonly IMonitorProvider _provider;

        public GetMonitorsHandler(IMonitorProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetMonitors message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IMessage>(new GetMonitorsResponse
            {
                Number = _provider.GetMonitorCount()
            });
        }
    }
}
