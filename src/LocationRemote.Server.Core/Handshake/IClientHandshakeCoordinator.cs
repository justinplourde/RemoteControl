using LocationRemote.Server.Core.Sessions;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Handshake
{
    public interface IClientHandshakeCoordinator
    {
        Task<ClientHandshakeResult> IdentifyAsync(
            string connectionId,
            IRemoteClientSession session,
            CancellationToken cancellationToken);
    }
}
