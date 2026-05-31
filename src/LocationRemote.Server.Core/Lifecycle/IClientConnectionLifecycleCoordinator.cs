using LocationRemote.Server.Core.Sessions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Lifecycle
{
    public interface IClientConnectionLifecycleCoordinator
    {
        Task ConnectedAsync(string connectionId, CancellationToken cancellationToken);

        Task IdentifiedAsync(string connectionId, IRemoteClientSession session, CancellationToken cancellationToken);

        Task DisconnectedAsync(string connectionId, string clientId, string reason, CancellationToken cancellationToken);

        Task FaultedAsync(string connectionId, string clientId, Exception exception, CancellationToken cancellationToken);
    }
}
