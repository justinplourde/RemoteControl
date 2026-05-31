using LocationRemote.Server.Core.Sessions;
using Quasar.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Listeners
{
    public interface IRemoteClientConnection : IRemoteClientSession
    {
        string ConnectionId { get; }

        bool IsIdentified { get; }

        void SetIdentification(ClientIdentification identification);

        Task DisconnectAsync(string reason, CancellationToken cancellationToken);
    }
}
