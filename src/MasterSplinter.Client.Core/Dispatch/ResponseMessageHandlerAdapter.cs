using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Dispatch
{
    public sealed class ResponseMessageHandlerAdapter<TMessage> : IMessageHandler<TMessage>
        where TMessage : IMessage
    {
        private readonly IResponseMessageHandler<TMessage> _handler;

        public ResponseMessageHandlerAdapter(IResponseMessageHandler<TMessage> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public async Task HandleAsync(IClientContext context, TMessage message, CancellationToken cancellationToken)
        {
            if (!(context is IClientCommandContext commandContext))
                throw new InvalidOperationException("Response handlers require a command context that can send messages.");

            IMessage response = await _handler.HandleAsync(context, message, cancellationToken).ConfigureAwait(false);
            if (response != null)
                await commandContext.SendAsync(response, cancellationToken).ConfigureAwait(false);
        }
    }
}
