using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoAskElevateHandler : IResponseMessageHandler<DoAskElevate>
    {
        private readonly IElevationRequestProvider _provider;

        public DoAskElevateHandler(IElevationRequestProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoAskElevate message, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ElevationRequestResult result = _provider.RequestElevation();
            string status;
            switch (result.Status)
            {
                case ElevationRequestStatus.AlreadyElevated:
                    status = "Process already elevated.";
                    break;
                case ElevationRequestStatus.Requested:
                    status = "Elevation requested.";
                    break;
                case ElevationRequestStatus.Refused:
                    status = "User refused the elevation request.";
                    break;
                default:
                    status = $"Elevation request failed: {result.ErrorMessage}";
                    break;
            }

            return Task.FromResult<IMessage>(new SetStatus { Message = status });
        }
    }
}
