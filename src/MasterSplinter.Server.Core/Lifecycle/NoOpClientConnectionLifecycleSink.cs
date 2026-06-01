using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Lifecycle
{
    public sealed class NoOpClientConnectionLifecycleSink : IClientConnectionLifecycleSink
    {
        public static readonly NoOpClientConnectionLifecycleSink Instance = new NoOpClientConnectionLifecycleSink();

        private NoOpClientConnectionLifecycleSink()
        {
        }

        public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
