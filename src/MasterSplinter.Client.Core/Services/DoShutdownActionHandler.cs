using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoShutdownActionHandler : IResponseMessageHandler<DoShutdownAction>
    {
        private readonly IShutdownActionProvider _provider;

        public DoShutdownActionHandler(IShutdownActionProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoShutdownAction message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            ShutdownActionResult result = _provider.RequestAction(message.Action);
            string status = result.IsSuccess
                ? $"{FormatAction(message.Action)} requested."
                : $"Action failed: {result.ErrorMessage}";

            return Task.FromResult<IMessage>(new SetStatus { Message = status });
        }

        private static string FormatAction(ShutdownAction action)
        {
            switch (action)
            {
                case ShutdownAction.Shutdown:
                    return "Shutdown";
                case ShutdownAction.Restart:
                    return "Restart";
                case ShutdownAction.Standby:
                    return "Standby";
                default:
                    return action.ToString();
            }
        }
    }
}
