using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using Quasar.Common.Messages;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Host
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
                    bool dispatchOnce = !string.IsNullOrWhiteSpace(options.DispatchCommand);
                    var onceCompletion = options.Once || dispatchOnce ? new TaskCompletionSource<bool>() : null;
                    var responseCompletion = dispatchOnce ? new TaskCompletionSource<IMessage>() : null;
                    var lifecycleSink = new ConsoleLifecycleSink(onceCompletion);
                    var lifecycle = new ClientConnectionLifecycleCoordinator(registry, lifecycleSink);
                    var handshake = new ClientHandshakeCoordinator(lifecycle);
                    var listener = new LoopbackTcpRemoteClientListener();
                    var orchestrator = new RemoteClientListenerOrchestrator(
                        listener,
                        lifecycle,
                        handshake,
                        new ConsoleRemoteClientMessageSink(responseCompletion));

                    await orchestrator.StartAsync(
                        new ServerListenOptions(options.Host, options.Port),
                        tokenSource.Token).ConfigureAwait(false);

                    Console.WriteLine($"MasterSplinter server host listening on {options.Host}:{options.Port}.");
                    Console.WriteLine("Loopback TCP transport active; press Ctrl+C to stop.");

                    if (options.SmokeTest)
                    {
                        await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
                        Console.WriteLine("Smoke test completed.");
                        return 0;
                    }

                    if (dispatchOnce)
                    {
                        using (tokenSource.Token.Register(() => onceCompletion.TrySetCanceled()))
                        {
                            try
                            {
                                await onceCompletion.Task.ConfigureAwait(false);
                                ClientSessionSnapshot session = registry.GetSnapshots().FirstOrDefault();
                                if (session == null)
                                    throw new InvalidOperationException("No identified client is available for dispatch.");

                                IMessage command = CreateDispatchCommand(options.DispatchCommand);
                                var dispatcher = new ServerCommandDispatcher(registry);
                                CommandDispatchResult dispatchResult = await dispatcher.DispatchAsync(
                                    session.ClientId,
                                    command,
                                    tokenSource.Token).ConfigureAwait(false);
                                Console.WriteLine($"Dispatch result: {dispatchResult.Status}.");

                                if (dispatchResult.Status == CommandDispatchStatus.Sent)
                                {
                                    IMessage response = await responseCompletion.Task.WaitAsync(
                                        TimeSpan.FromSeconds(15),
                                        tokenSource.Token).ConfigureAwait(false);
                                    Console.WriteLine($"Dispatch response: {response.GetType().Name}.");
                                }
                            }
                            catch (TaskCanceledException)
                            {
                            }
                        }
                    }
                    else if (onceCompletion != null)
                    {
                        using (tokenSource.Token.Register(() => onceCompletion.TrySetCanceled()))
                        {
                            try
                            {
                                await onceCompletion.Task.ConfigureAwait(false);
                            }
                            catch (TaskCanceledException)
                            {
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            await Task.Delay(Timeout.InfiniteTimeSpan, tokenSource.Token).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException)
                        {
                        }
                    }

                    await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
                    Console.WriteLine("MasterSplinter server host stopped.");
                    return 0;
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        private static IMessage CreateDispatchCommand(string commandName)
        {
            if (string.Equals(commandName, "get-system-info", StringComparison.OrdinalIgnoreCase))
                return new GetSystemInfo();

            throw new ArgumentException($"Unknown dispatch command '{commandName}'.");
        }
    }
}
