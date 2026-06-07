using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Shell
{
    public sealed class DoShellExecuteHandler : IResponseMessageHandler<DoShellExecute>
    {
        private readonly IShellCommandProvider _provider;

        public DoShellExecuteHandler(IShellCommandProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public async Task<IMessage> HandleAsync(IClientContext context, DoShellExecute message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            ShellCommandResult result = await _provider.ExecuteAsync(message.Command, cancellationToken)
                .ConfigureAwait(false);

            return new DoShellExecuteResponse
            {
                Output = result.Output,
                IsError = !result.IsSuccess
            };
        }
    }
}
