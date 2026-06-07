using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Dispatch
{
    public interface IClientLifecycleContext : IClientCommandContext
    {
        Task RequestDisconnectAsync(CancellationToken cancellationToken);

        Task RequestReconnectAsync(CancellationToken cancellationToken);
    }
}
