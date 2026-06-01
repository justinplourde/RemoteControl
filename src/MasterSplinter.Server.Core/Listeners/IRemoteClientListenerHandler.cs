using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Listeners
{
    public interface IRemoteClientListenerHandler
    {
        Task ClientConnectedAsync(IRemoteClientConnection connection, CancellationToken cancellationToken);

        Task MessageReceivedAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken);

        Task ClientDisconnectedAsync(
            IRemoteClientConnection connection,
            string reason,
            CancellationToken cancellationToken);

        Task ClientFaultedAsync(
            IRemoteClientConnection connection,
            Exception exception,
            CancellationToken cancellationToken);
    }
}
