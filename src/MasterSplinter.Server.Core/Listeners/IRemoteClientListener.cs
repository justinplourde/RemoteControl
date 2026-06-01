using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Listeners
{
    public interface IRemoteClientListener
    {
        bool IsListening { get; }

        ServerListenOptions Options { get; }

        Task StartAsync(
            ServerListenOptions options,
            IRemoteClientListenerHandler handler,
            CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}
