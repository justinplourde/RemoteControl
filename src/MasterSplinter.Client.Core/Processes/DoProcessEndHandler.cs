using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Processes
{
    public sealed class DoProcessEndHandler : IResponseMessageHandler<DoProcessEnd>
    {
        private readonly IProcessEndProvider _provider;

        public DoProcessEndHandler(IProcessEndProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoProcessEnd message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            ProcessEndResult result = _provider.EndProcess(message.Pid);
            return Task.FromResult<IMessage>(new DoProcessResponse
            {
                Action = ProcessAction.End,
                Result = result.IsSuccess
            });
        }
    }
}
