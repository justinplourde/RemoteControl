using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Listeners
{
    public interface IRemoteClientMessageSink
    {
        Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken);
    }
}
