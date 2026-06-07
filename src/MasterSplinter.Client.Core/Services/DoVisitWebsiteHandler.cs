using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class DoVisitWebsiteHandler : IResponseMessageHandler<DoVisitWebsite>
    {
        private readonly IWebsiteVisitProvider _provider;

        public DoVisitWebsiteHandler(IWebsiteVisitProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public Task<IMessage> HandleAsync(IClientContext context, DoVisitWebsite message, CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _provider.Visit(message.Url, message.Hidden);
                return Task.FromResult<IMessage>(new SetStatus { Message = "Visited Website" });
            }
            catch (Exception exception)
            {
                return Task.FromResult<IMessage>(new SetStatus { Message = $"Visit Website failed: {exception.Message}" });
            }
        }
    }
}
