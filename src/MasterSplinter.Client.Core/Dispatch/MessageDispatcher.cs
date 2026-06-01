using Quasar.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Dispatch
{
    public sealed class MessageDispatcher : IMessageDispatcher
    {
        private readonly IReadOnlyDictionary<Type, Func<IClientContext, IMessage, CancellationToken, Task>> _handlers;

        private MessageDispatcher(IReadOnlyDictionary<Type, Func<IClientContext, IMessage, CancellationToken, Task>> handlers)
        {
            _handlers = handlers;
        }

        public async Task<DispatchResult> DispatchAsync(IClientContext context, IMessage message, CancellationToken cancellationToken)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!_handlers.TryGetValue(message.GetType(), out var handler))
                return DispatchResult.Unhandled();

            try
            {
                await handler(context, message, cancellationToken).ConfigureAwait(false);
                return DispatchResult.Handled();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                return DispatchResult.Faulted(exception);
            }
        }

        public sealed class Builder
        {
            private readonly Dictionary<Type, Func<IClientContext, IMessage, CancellationToken, Task>> _handlers =
                new Dictionary<Type, Func<IClientContext, IMessage, CancellationToken, Task>>();

            public Builder AddHandler<TMessage>(IMessageHandler<TMessage> handler) where TMessage : IMessage
            {
                if (handler == null)
                    throw new ArgumentNullException(nameof(handler));

                Type messageType = typeof(TMessage);
                if (_handlers.ContainsKey(messageType))
                    throw new InvalidOperationException($"A handler is already registered for message type '{messageType.FullName}'.");

                _handlers.Add(messageType, (context, message, cancellationToken) =>
                    handler.HandleAsync(context, (TMessage)message, cancellationToken));

                return this;
            }

            public MessageDispatcher Build()
            {
                return new MessageDispatcher(new Dictionary<Type, Func<IClientContext, IMessage, CancellationToken, Task>>(_handlers));
            }
        }
    }
}
