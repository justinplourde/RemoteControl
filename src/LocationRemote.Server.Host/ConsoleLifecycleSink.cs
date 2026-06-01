using LocationRemote.Server.Core.Lifecycle;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Host
{
    internal sealed class ConsoleLifecycleSink : IClientConnectionLifecycleSink
    {
        private readonly TaskCompletionSource<bool> _completion;

        public ConsoleLifecycleSink()
            : this(null)
        {
        }

        public ConsoleLifecycleSink(TaskCompletionSource<bool> completion)
        {
            _completion = completion;
        }

        public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
        {
            Console.WriteLine(
                $"[{lifecycleEvent.OccurredAtUtc:u}] {lifecycleEvent.Kind} connection={lifecycleEvent.ConnectionId} client={lifecycleEvent.ClientId ?? "-"}");

            if (lifecycleEvent.Kind == ClientConnectionLifecycleEventKind.Identified ||
                lifecycleEvent.Kind == ClientConnectionLifecycleEventKind.Faulted)
            {
                CompleteAfterHandshakeResponseCanFlush();
            }

            return Task.CompletedTask;
        }

        private void CompleteAfterHandshakeResponseCanFlush()
        {
            if (_completion == null)
                return;

            _ = Task.Run(async () =>
            {
                await Task.Delay(250).ConfigureAwait(false);
                _completion.TrySetResult(true);
            });
        }
    }
}
