using MasterSplinter.Server.Core.Listeners;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Host
{
    internal sealed class ConsoleRemoteClientMessageSink : IRemoteClientMessageSink
    {
        private readonly TaskCompletionSource<IMessage> _completion;

        public ConsoleRemoteClientMessageSink()
            : this(null)
        {
        }

        public ConsoleRemoteClientMessageSink(TaskCompletionSource<IMessage> completion)
        {
            _completion = completion;
        }

        public Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"Received {message.GetType().Name} from client {connection.ClientId ?? "-"} on {connection.ConnectionId}.");
            _completion?.TrySetResult(message);

            return Task.CompletedTask;
        }
    }
}
