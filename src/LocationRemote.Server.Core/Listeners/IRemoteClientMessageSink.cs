using Quasar.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Listeners
{
    public interface IRemoteClientMessageSink
    {
        Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken);
    }
}
