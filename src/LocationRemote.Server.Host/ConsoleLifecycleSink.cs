using LocationRemote.Server.Core.Lifecycle;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Host
{
    internal sealed class ConsoleLifecycleSink : IClientConnectionLifecycleSink
    {
        public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"[{lifecycleEvent.OccurredAtUtc:u}] {lifecycleEvent.Kind} connection={lifecycleEvent.ConnectionId} client={lifecycleEvent.ClientId ?? "-"}");

            return Task.CompletedTask;
        }
    }
}
