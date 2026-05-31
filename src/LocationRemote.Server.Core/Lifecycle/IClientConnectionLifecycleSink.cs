using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Lifecycle
{
    public interface IClientConnectionLifecycleSink
    {
        Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken);
    }
}
