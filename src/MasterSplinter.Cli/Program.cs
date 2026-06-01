using MasterSplinter.Server.Core.Authorization;
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

                IMessage command = CreateMessage(options);
                CommandDispatchRequest request = await CreateAuthorizedRequestAsync(
                    options,
                    session.ClientId,
                    command,
                    cancellationToken).ConfigureAwait(false);

                CommandDispatchResult result = await new ServerCommandDispatcher(registry)
                    .DispatchAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                Console.WriteLine(
                    $"Dispatch result: {result.Status}. Safety={result.SafetyMetadata.SafetyClass}; RequiresPermission={result.SafetyMetadata.RequiresPermission}; RequiresConsent={result.SafetyMetadata.RequiresConsent}.");

                if (result.Status != CommandDispatchStatus.Sent)
                    return 2;

                IMessage reply = await response.Task.WaitAsync(
                    TimeSpan.FromSeconds(options.TimeoutSeconds),
                    cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"Dispatch response: {reply.GetType().Name}.");
                PrintResponseSummary(reply);
                return 0;
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }
        }

        public static IMessage CreateMessage(CliOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            string dispatchCommand = options.DispatchCommand;
            if (string.Equals(dispatchCommand, "get-system-info", StringComparison.OrdinalIgnoreCase))
                return new GetSystemInfo();
            if (string.Equals(dispatchCommand, "get-drives", StringComparison.OrdinalIgnoreCase))
                return new GetDrives();
            if (string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase))
                return new GetDirectory { RemotePath = options.Path };
            if (string.Equals(dispatchCommand, "get-processes", StringComparison.OrdinalIgnoreCase))
                return new GetProcesses();
            if (string.Equals(dispatchCommand, "get-startup-items", StringComparison.OrdinalIgnoreCase))
                return new GetStartupItems();
            if (string.Equals(dispatchCommand, "get-connections", StringComparison.OrdinalIgnoreCase))
                return new GetConnections();

            throw new ArgumentException($"Unknown dispatch command '{dispatchCommand}'.");
        }

        private static async Task<CommandDispatchRequest> CreateAuthorizedRequestAsync(
            CliOptions options,
            string clientId,
            IMessage command,
            CancellationToken cancellationToken)
        {
            var request = new CommandDispatchRequest(
                Guid.NewGuid(),
                clientId,
                command,
                options.OperatorId,
                "cli");

            CommandSafetyMetadata safetyMetadata = DefaultCommandSafetyClassifier.Instance.Classify(command);
            var authorizationService = new CommandAuthorizationService(
                new CliOperatorPermissionService(options.GrantPermission),
                new CliClientConsentService(options.GrantConsent));
            CommandDispatchAuthorization authorization = await authorizationService.AuthorizeAsync(
                new OperatorIdentity(options.OperatorId, options.OperatorId),
                request,
                safetyMetadata,
                cancellationToken).ConfigureAwait(false);

            return request.WithAuthorization(authorization);
        }

        private static void PrintResponseSummary(IMessage reply)
        {
            switch (reply)
            {
                case GetSystemInfoResponse response:
                    Console.WriteLine($"System info entries: {Count(response.SystemInfos)}.");
                    break;
                case GetDrivesResponse response:
                    Console.WriteLine($"Drives: {Count(response.Drives)}.");
                    break;
                case GetDirectoryResponse response:
                    Console.WriteLine($"Directory path: {response.RemotePath ?? "-"}; Items: {Count(response.Items)}.");
                    break;
                case GetProcessesResponse response:
                    Console.WriteLine($"Processes: {Count(response.Processes)}.");
                    break;
                case GetStartupItemsResponse response:
                    Console.WriteLine($"Startup items: {Count(response.StartupItems)}.");
                    break;
                case GetConnectionsResponse response:
                    Console.WriteLine($"TCP connections: {Count(response.Connections)}.");
                    break;
            }
        }

        private static int Count(Array items)
        {
            return items == null ? 0 : items.Length;
        }

        private static int Count(System.Collections.ICollection items)
        {
            return items == null ? 0 : items.Count;
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

        private sealed class CliOperatorPermissionService : IOperatorPermissionService
        {
            private readonly bool _grantPermission;

            public CliOperatorPermissionService(bool grantPermission)
            {
                _grantPermission = grantPermission;
            }

            public Task<bool> HasPermissionAsync(
                OperatorIdentity operatorIdentity,
                OperatorPermission permission,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_grantPermission);
            }
        }

        private sealed class CliClientConsentService : IClientConsentService
        {
            private readonly bool _grantConsent;

            public CliClientConsentService(bool grantConsent)
            {
                _grantConsent = grantConsent;
            }

            public Task<bool> HasConsentAsync(
                string clientId,
                OperatorIdentity operatorIdentity,
                CommandDispatchRequest request,
                CommandSafetyMetadata safetyMetadata,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_grantConsent);
            }
        }
    }
}
