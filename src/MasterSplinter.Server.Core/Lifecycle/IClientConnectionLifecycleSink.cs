using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Lifecycle
{
    public interface IClientConnectionLifecycleSink
    {
        Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken);
    }
}
