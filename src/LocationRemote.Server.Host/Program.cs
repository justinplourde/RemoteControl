using LocationRemote.Server.Core.Handshake;
using LocationRemote.Server.Core.Lifecycle;
using LocationRemote.Server.Core.Listeners;
using LocationRemote.Server.Core.Sessions;
using LocationRemote.Server.Host;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Host
{
    internal static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                HostOptions options = HostOptions.Parse(args);
                using (var tokenSource = new CancellationTokenSource())
                {
                    Console.CancelKeyPress += (sender, eventArgs) =>
                    {
                        eventArgs.Cancel = true;
                        tokenSource.Cancel();
                    };

                    var registry = new ClientSessionRegistry();
                    var lifecycleSink = new ConsoleLifecycleSink();
                    var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
                    var handshake = new ClientHandshakeCoordinator(lifecycle);
                    var listener = new IdleRemoteClientListener();
                    var orchestrator = new RemoteClientListenerOrchestrator(
                        listener,
                        lifecycle,
                        handshake,
                        new ConsoleRemoteClientMessageSink());

                    await orchestrator.StartAsync(
                        new ServerListenOptions(options.Host, options.Port),
                        tokenSource.Token).ConfigureAwait(false);

                    Console.WriteLine($"LocationRemote server host listening placeholder on {options.Host}:{options.Port}.");
                    Console.WriteLine("Transport implementation is pending; press Ctrl+C to stop.");

                    if (options.SmokeTest)
                    {
                        await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
                        Console.WriteLine("Smoke test completed.");
                        return 0;
                    }

                    try
                    {
                        await Task.Delay(Timeout.InfiniteTimeSpan, tokenSource.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                    }

                    await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("LocationRemote server host stopped.");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }
    }
}
