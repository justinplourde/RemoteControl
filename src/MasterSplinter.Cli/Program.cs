using MasterSplinter.Server.Core.Authorization;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using Quasar.Common.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

                if (string.Equals(options.Command, "listen", StringComparison.OrdinalIgnoreCase))
                    return await RunListenAsync(options, CancellationToken.None).ConfigureAwait(false);

                return await RunDispatchAsync(options, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        private static async Task<int> RunListenAsync(CliOptions options, CancellationToken cancellationToken)
        {
            var registry = new ClientSessionRegistry();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, new CompletionLifecycleSink(null));
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var responseSink = new AwaitableMessageSink();
            var orchestrator = new RemoteClientListenerOrchestrator(
                listener,
                lifecycle,
                handshake,
                responseSink);
            var dispatcher = new ServerCommandDispatcher(registry);

            await orchestrator.StartAsync(
                new ServerListenOptions(options.Host, options.Port),
                cancellationToken).ConfigureAwait(false);

            Console.WriteLine($"Listening on {options.Host}:{options.Port}.");
            PrintListenHelp();

            try
            {
                while (true)
                {
                    Console.Write("> ");
                    string line = await Console.In.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                        break;

                    ListenCommand command;
                    try
                    {
                        command = ListenCommand.Parse(line);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                        continue;
                    }

                    if (string.Equals(command.Verb, "empty", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (string.Equals(command.Verb, "exit", StringComparison.OrdinalIgnoreCase))
                        break;
                    if (string.Equals(command.Verb, "help", StringComparison.OrdinalIgnoreCase))
                    {
                        PrintListenHelp();
                        continue;
                    }
                    if (string.Equals(command.Verb, "clients", StringComparison.OrdinalIgnoreCase))
                    {
                        PrintClients(registry);
                        continue;
                    }

                    await DispatchFromListenAsync(
                        options,
                        registry,
                        dispatcher,
                        responseSink,
                        command,
                        cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                await orchestrator.StopAsync(CancellationToken.None).ConfigureAwait(false);
            }

            return 0;
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
                PrintResponse(reply);
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

            return CreateMessage(options.DispatchCommand, options.Path);
        }

        public static IMessage CreateMessage(string dispatchCommand, string path)
        {
            if (string.Equals(dispatchCommand, "get-system-info", StringComparison.OrdinalIgnoreCase))
                return new GetSystemInfo();
            if (string.Equals(dispatchCommand, "get-drives", StringComparison.OrdinalIgnoreCase))
                return new GetDrives();
            if (string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase))
                return new GetDirectory { RemotePath = path };
            if (string.Equals(dispatchCommand, "get-processes", StringComparison.OrdinalIgnoreCase))
                return new GetProcesses();
            if (string.Equals(dispatchCommand, "get-startup-items", StringComparison.OrdinalIgnoreCase))
                return new GetStartupItems();
            if (string.Equals(dispatchCommand, "get-connections", StringComparison.OrdinalIgnoreCase))
                return new GetConnections();

            throw new ArgumentException($"Unknown dispatch command '{dispatchCommand}'.");
        }

        private static async Task DispatchFromListenAsync(
            CliOptions options,
            ClientSessionRegistry registry,
            ServerCommandDispatcher dispatcher,
            AwaitableMessageSink responseSink,
            ListenCommand listenCommand,
            CancellationToken cancellationToken)
        {
            string clientId = ResolveClientId(registry, listenCommand.ClientId);
            IMessage message = CreateMessage(listenCommand.DispatchCommand, listenCommand.Path);
            CommandDispatchRequest request = await CreateAuthorizedRequestAsync(
                options,
                clientId,
                message,
                cancellationToken).ConfigureAwait(false);

            Task<IMessage> responseTask = responseSink.WaitForNextAsync(clientId);
            CommandDispatchResult result = await dispatcher.DispatchAsync(request, cancellationToken)
                .ConfigureAwait(false);
            Console.WriteLine(
                $"Dispatch result: {result.Status}. Safety={result.SafetyMetadata.SafetyClass}; RequiresPermission={result.SafetyMetadata.RequiresPermission}; RequiresConsent={result.SafetyMetadata.RequiresConsent}.");

            if (result.Status != CommandDispatchStatus.Sent)
            {
                responseSink.CancelWait(clientId);
                return;
            }

            IMessage response;
            try
            {
                response = await responseTask.WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                responseSink.CancelWait(clientId);
                throw;
            }

            Console.WriteLine($"Dispatch response: {response.GetType().Name}.");
            PrintResponse(response);
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

        private static string ResolveClientId(ClientSessionRegistry registry, string clientId)
        {
            IReadOnlyList<ClientSessionSnapshot> snapshots = registry.GetSnapshots();
            if (snapshots.Count == 0)
                throw new InvalidOperationException("No identified clients are connected.");

            if (string.Equals(clientId, "first", StringComparison.OrdinalIgnoreCase))
                return snapshots[0].ClientId;

            if (snapshots.Any(snapshot => string.Equals(snapshot.ClientId, clientId, StringComparison.OrdinalIgnoreCase)))
                return clientId;

            throw new InvalidOperationException($"Client '{clientId}' is not connected.");
        }

        private static void PrintClients(ClientSessionRegistry registry)
        {
            IReadOnlyList<ClientSessionSnapshot> snapshots = registry.GetSnapshots();
            Console.WriteLine($"Clients: {snapshots.Count}.");
            foreach (ClientSessionSnapshot snapshot in snapshots)
            {
                string user = snapshot.Identification == null ? "-" : ValueOrDash(snapshot.Identification.Username);
                string machine = snapshot.Identification == null ? "-" : ValueOrDash(snapshot.Identification.PcName);
                Console.WriteLine($"- {snapshot.ClientId} Connected={snapshot.IsConnected} User={user} Machine={machine}");
            }
        }

        private static void PrintListenHelp()
        {
            Console.WriteLine("Commands: clients | dispatch <client-id|first> <command> [--path <path>] | help | exit");
        }

        public static string[] FormatResponse(IMessage reply)
        {
            if (reply == null)
                throw new ArgumentNullException(nameof(reply));

            var lines = new List<string>();
            switch (reply)
            {
                case GetSystemInfoResponse response:
                    lines.Add($"System info entries: {Count(response.SystemInfos)}.");
                    if (response.SystemInfos != null)
                    {
                        foreach (Tuple<string, string> item in response.SystemInfos)
                            lines.Add($"- {ValueOrDash(item.Item1)}: {ValueOrDash(item.Item2)}");
                    }
                    break;
                case GetDrivesResponse response:
                    lines.Add($"Drives: {Count(response.Drives)}.");
                    if (response.Drives != null)
                    {
                        foreach (Quasar.Common.Models.Drive drive in response.Drives)
                            lines.Add($"- {ValueOrDash(drive.DisplayName)} => {ValueOrDash(drive.RootDirectory)}");
                    }
                    break;
                case GetDirectoryResponse response:
                    lines.Add($"Directory path: {ValueOrDash(response.RemotePath)}; Items: {Count(response.Items)}.");
                    if (response.Items != null)
                    {
                        foreach (Quasar.Common.Models.FileSystemEntry item in response.Items)
                            lines.Add($"- {item.EntryType} {ValueOrDash(item.Name)} Size={item.Size}");
                    }
                    break;
                case GetProcessesResponse response:
                    lines.Add($"Processes: {Count(response.Processes)}.");
                    if (response.Processes != null)
                    {
                        foreach (Quasar.Common.Models.Process process in response.Processes)
                            lines.Add($"- PID={process.Id} {ValueOrDash(process.Name)} Title={ValueOrDash(process.MainWindowTitle)}");
                    }
                    break;
                case GetStartupItemsResponse response:
                    lines.Add($"Startup items: {Count(response.StartupItems)}.");
                    if (response.StartupItems != null)
                    {
                        foreach (Quasar.Common.Models.StartupItem item in response.StartupItems)
                            lines.Add($"- {item.Type} {ValueOrDash(item.Name)} => {ValueOrDash(item.Path)}");
                    }
                    break;
                case GetConnectionsResponse response:
                    lines.Add($"TCP connections: {Count(response.Connections)}.");
                    if (response.Connections != null)
                    {
                        foreach (Quasar.Common.Models.TcpConnection connection in response.Connections)
                        {
                            lines.Add(
                                $"- {ValueOrDash(connection.ProcessName)} {ValueOrDash(connection.LocalAddress)}:{connection.LocalPort} -> {ValueOrDash(connection.RemoteAddress)}:{connection.RemotePort} {connection.State}");
                        }
                    }
                    break;
                default:
                    lines.Add($"No formatter for {reply.GetType().Name}.");
                    break;
            }

            return lines.ToArray();
        }

        private static void PrintResponse(IMessage reply)
        {
            foreach (string line in FormatResponse(reply))
                Console.WriteLine(line);
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
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
                    _identified?.TrySetResult(true);

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

        private sealed class AwaitableMessageSink : IRemoteClientMessageSink
        {
            private readonly ConcurrentDictionary<string, TaskCompletionSource<IMessage>> _pending =
                new ConcurrentDictionary<string, TaskCompletionSource<IMessage>>(StringComparer.OrdinalIgnoreCase);

            public Task<IMessage> WaitForNextAsync(string clientId)
            {
                if (string.IsNullOrWhiteSpace(clientId))
                    throw new ArgumentException("Client id is required.", nameof(clientId));

                var pending = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (!_pending.TryAdd(clientId, pending))
                    throw new InvalidOperationException($"A dispatch is already waiting for client '{clientId}'.");

                return pending.Task;
            }

            public void CancelWait(string clientId)
            {
                if (_pending.TryRemove(clientId, out TaskCompletionSource<IMessage> pending))
                    pending.TrySetCanceled();
            }

            public Task HandleAsync(
                IRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                string clientId = connection.ClientId ?? string.Empty;
                Console.WriteLine(
                    $"Received {message.GetType().Name} from client {connection.ClientId ?? "-"} on {connection.ConnectionId}.");

                if (_pending.TryRemove(clientId, out TaskCompletionSource<IMessage> pending))
                    pending.TrySetResult(message);

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
