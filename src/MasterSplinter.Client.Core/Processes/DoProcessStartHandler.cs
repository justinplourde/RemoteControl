using MasterSplinter.Client.Core.Dispatch;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Processes
{
    public sealed class DoProcessStartHandler : IResponseMessageHandler<DoProcessStart>
    {
        private readonly IProcessStartProvider _provider;

        public DoProcessStartHandler(IProcessStartProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoProcessStart message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();
            ProcessStartResult result = string.IsNullOrWhiteSpace(message.DownloadUrl) && !message.IsUpdate
                ? _provider.StartProcess(message.FilePath)
                : ProcessStartResult.Error();

            return Task.FromResult<IMessage>(new DoProcessResponse
            {
                Action = ProcessAction.Start,
                Result = result.IsSuccess
            });
        }
    }
}
