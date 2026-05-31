using LocationRemote.Server.Core.Listeners;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Host
{
    internal sealed class ConsoleRemoteClientMessageSink : IRemoteClientMessageSink
    {
        public Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"Received {message.GetType().Name} from client {connection.ClientId ?? "-"} on {connection.ConnectionId}.");

            return Task.CompletedTask;
        }
    }
}
