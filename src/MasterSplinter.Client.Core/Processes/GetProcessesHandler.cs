using MasterSplinter.Client.Core.Dispatch;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Processes
{
    public sealed class GetProcessesHandler : IResponseMessageHandler<GetProcesses>
    {
        private readonly IProcessProvider _provider;

        public GetProcessesHandler(IProcessProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, GetProcesses message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult<IMessage>(new GetProcessesResponse
            {
                Processes = _provider.GetProcesses()
            });
        }
    }
}
