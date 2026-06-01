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

namespace MasterSplinter.Cli
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                CliOptions options = CliOptions.Parse(args);
                if (options.ShowHelp)
                {
                    Console.WriteLine(CliOptions.Usage);
                    return 0;
                }

                if (!string.Equals(options.Command, "dispatch", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException($"Unknown command '{options.Command}'.");

                return await RunDispatchAsync(options, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        private static async Task<int> RunDispatchAsync(CliOptions options, CancellationToken cancellationToken)
        {
            var registry = new ClientSessionRegistry();
            var identified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var response = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, new CompletionLifecycleSink(identified));
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var orchestrator = new RemoteClientListenerOrchestrator(
                listener,
                lifecycle,
                handshake,
                new CompletionMessageSink(response));

            await orchestrator.StartAsync(
                new ServerListenOptions(options.Host, options.Port),
                cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Waiting for one client on {options.Host}:{options.Port}.");
            try
            {
                await identified.Task.WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken)
                    .ConfigureAwait(false);

                ClientSessionSnapshot session = registry.GetSnapshots().FirstOrDefault();
                if (session == null)
                    throw new InvalidOperationException("No identified client is available for dispatch.");

                IMessage command = CreateMessage(options.DispatchCommand);
                CommandDispatchResult result = await new ServerCommandDispatcher(registry)
                    .DispatchAsync(session.ClientId, command, cancellationToken)
                    .ConfigureAwait(false);
                Console.WriteLine($"Dispatch result: {result.Status}.");

                if (result.Status != CommandDispatchStatus.Sent)
                    return 2;

                IMessage reply = await response.Task.WaitAsync(
                    TimeSpan.FromSeconds(options.TimeoutSeconds),
                    cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Dispatch response: {reply.GetType().Name}.");
                return 0;
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        private static IMessage CreateMessage(string dispatchCommand)
        {
            if (string.Equals(dispatchCommand, "get-system-info", StringComparison.OrdinalIgnoreCase))
                return new GetSystemInfo();

            throw new ArgumentException($"Unknown dispatch command '{dispatchCommand}'.");
        }

        private sealed class CompletionLifecycleSink : IClientConnectionLifecycleSink
        {
            private readonly TaskCompletionSource<bool> _identified;

            public CompletionLifecycleSink(TaskCompletionSource<bool> identified)
            {
                _identified = identified;
            }

            public Task WriteAsync(ClientConnectionLifecycleEvent lifecycleEvent, CancellationToken cancellationToken)
            {
                Console.WriteLine(
                    $"[{lifecycleEvent.OccurredAtUtc:u}] {lifecycleEvent.Kind} connection={lifecycleEvent.ConnectionId} client={lifecycleEvent.ClientId ?? "-"}");

                if (lifecycleEvent.Kind == ClientConnectionLifecycleEventKind.Identified)
                    _identified.TrySetResult(true);

                return Task.CompletedTask;
            }
        }

        private sealed class CompletionMessageSink : IRemoteClientMessageSink
        {
            private readonly TaskCompletionSource<IMessage> _response;

            public CompletionMessageSink(TaskCompletionSource<IMessage> response)
            {
                _response = response;
            }

            public Task HandleAsync(
                IRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                Console.WriteLine(
                    $"Received {message.GetType().Name} from client {connection.ClientId ?? "-"} on {connection.ConnectionId}.");
                _response.TrySetResult(message);
                return Task.CompletedTask;
            }
        }
    }
}
