using MasterSplinter.Server.Core.Authorization;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Cli
{
    public static class Program
    {
        private const int FileTransferId = 1;
        private const int UploadChunkSize = 64 * 1024;
        private const long MaxUploadFileSizeBytes = 100L * 1024L * 1024L;

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
            var responseSink = new AwaitableMessageSink();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, new CompletionLifecycleSink(identified));
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var orchestrator = new RemoteClientListenerOrchestrator(
                listener,
                lifecycle,
                handshake,
                responseSink);

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

                if (IsUploadFileCommand(options.DispatchCommand))
                {
                    await DispatchUploadFileAsync(
                        options,
                        new ServerCommandDispatcher(registry),
                        responseSink,
                        session.ClientId,
                        options.Path,
                        options.RemotePath,
                        cancellationToken).ConfigureAwait(false);
                    return 0;
                }

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

                await ReceiveAndPrintResponseAsync(
                    options,
                    responseSink,
                    session.ClientId,
                    command,
                    options.OutputPath,
                    cancellationToken).ConfigureAwait(false);
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

            return CreateMessage(
                options.DispatchCommand,
                options.Path,
                options.NewPath,
                options.PathType,
                options.Pid,
                options.LocalAddress,
                options.LocalPort,
                options.RemoteAddress,
                options.RemotePort);
        }

        public static IMessage CreateMessage(string dispatchCommand, string path)
        {
            return CreateMessage(dispatchCommand, path, null, null, null, null, null, null, null);
        }

        public static IMessage CreateMessage(
            string dispatchCommand,
            string path,
            string newPath,
            string pathType,
            int? pid,
            string localAddress,
            ushort? localPort,
            string remoteAddress,
            ushort? remotePort)
        {
            if (IsUploadFileCommand(dispatchCommand))
                throw new ArgumentException("upload-file is a multi-message command and cannot be created as a single message.");
            if (string.Equals(dispatchCommand, "get-system-info", StringComparison.OrdinalIgnoreCase))
                return new GetSystemInfo();
            if (string.Equals(dispatchCommand, "get-drives", StringComparison.OrdinalIgnoreCase))
                return new GetDrives();
            if (string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase))
                return new GetDirectory { RemotePath = path };
            if (string.Equals(dispatchCommand, "get-registry-key", StringComparison.OrdinalIgnoreCase))
                return new DoLoadRegistryKey { RootKeyName = path };
            if (string.Equals(dispatchCommand, "download-file", StringComparison.OrdinalIgnoreCase))
                return new FileTransferRequest { Id = 1, RemotePath = path };
            if (string.Equals(dispatchCommand, "rename-path", StringComparison.OrdinalIgnoreCase))
                return new DoPathRename
                {
                    Path = path,
                    NewPath = newPath,
                    PathType = ParsePathType(pathType)
                };
            if (string.Equals(dispatchCommand, "delete-path", StringComparison.OrdinalIgnoreCase))
                return new DoPathDelete
                {
                    Path = path,
                    PathType = ParsePathType(pathType)
                };
            if (string.Equals(dispatchCommand, "start-process", StringComparison.OrdinalIgnoreCase))
                return new DoProcessStart { FilePath = path };
            if (string.Equals(dispatchCommand, "end-process", StringComparison.OrdinalIgnoreCase))
                return new DoProcessEnd { Pid = pid.GetValueOrDefault() };
            if (string.Equals(dispatchCommand, "close-connection", StringComparison.OrdinalIgnoreCase))
                return new DoCloseConnection
                {
                    LocalAddress = localAddress,
                    LocalPort = localPort.GetValueOrDefault(),
                    RemoteAddress = remoteAddress,
                    RemotePort = remotePort.GetValueOrDefault()
                };
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
            if (IsUploadFileCommand(listenCommand.DispatchCommand))
            {
                await DispatchUploadFileAsync(
                    options,
                    dispatcher,
                    responseSink,
                    clientId,
                    listenCommand.Path,
                    listenCommand.RemotePath,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            IMessage message = CreateMessage(
                listenCommand.DispatchCommand,
                listenCommand.Path,
                listenCommand.NewPath,
                listenCommand.PathType,
                listenCommand.Pid,
                listenCommand.LocalAddress,
                listenCommand.LocalPort,
                listenCommand.RemoteAddress,
                listenCommand.RemotePort);
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

            await ReceiveAndPrintResponseAsync(
                options,
                responseSink,
                clientId,
                message,
                listenCommand.OutputPath ?? options.OutputPath,
                responseTask,
                cancellationToken).ConfigureAwait(false);
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

        private static async Task DispatchUploadFileAsync(
            CliOptions options,
            ServerCommandDispatcher dispatcher,
            AwaitableMessageSink responseSink,
            string clientId,
            string localPath,
            string remotePath,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(localPath))
                throw new ArgumentException("Local upload path is required.", nameof(localPath));
            if (string.IsNullOrWhiteSpace(remotePath))
                throw new ArgumentException("Remote upload path is required.", nameof(remotePath));

            var fileInfo = new FileInfo(localPath);
            if (!fileInfo.Exists)
                throw new FileNotFoundException("Upload source file was not found.", localPath);
            if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                throw new InvalidOperationException("Upload source path is a directory.");
            if (fileInfo.Length > MaxUploadFileSizeBytes)
                throw new InvalidOperationException("Upload source file exceeds size limit.");

            Task<IMessage> responseTask = responseSink.WaitForNextAsync(clientId);
            long offset = 0;
            bool sentAnyChunk = false;
            using (FileStream input = File.Open(localPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var buffer = new byte[UploadChunkSize];
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    int read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0 && sentAnyChunk)
                        break;

                    byte[] data = new byte[read];
                    if (read > 0)
                        Buffer.BlockCopy(buffer, 0, data, 0, read);

                    var chunk = new FileTransferChunk
                    {
                        Id = FileTransferId,
                        FilePath = remotePath,
                        FileSize = fileInfo.Length,
                        Chunk = new MasterSplinter.Common.Models.FileChunk
                        {
                            Offset = offset,
                            Data = data
                        }
                    };
                    CommandDispatchRequest request = await CreateAuthorizedRequestAsync(
                        options,
                        clientId,
                        chunk,
                        cancellationToken).ConfigureAwait(false);

                    CommandDispatchResult result = await dispatcher.DispatchAsync(request, cancellationToken)
                        .ConfigureAwait(false);
                    Console.WriteLine(
                        $"Dispatch result: {result.Status}. Safety={result.SafetyMetadata.SafetyClass}; RequiresPermission={result.SafetyMetadata.RequiresPermission}; RequiresConsent={result.SafetyMetadata.RequiresConsent}.");

                    if (result.Status != CommandDispatchStatus.Sent)
                    {
                        responseSink.CancelWait(clientId);
                        return;
                    }

                    Console.WriteLine($"Upload chunk: Offset={offset}; Bytes={read}; Total={Math.Min(offset + read, fileInfo.Length)}/{fileInfo.Length}.");
                    sentAnyChunk = true;
                    offset += read;

                    if (read == 0 || offset >= fileInfo.Length)
                        break;
                }
            }

            await ReceiveUploadCompletionAsync(
                options,
                responseSink,
                clientId,
                FileTransferId,
                responseTask,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task ReceiveUploadCompletionAsync(
            CliOptions options,
            AwaitableMessageSink responseSink,
            string clientId,
            int transferId,
            Task<IMessage> responseTask,
            CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    IMessage response = await responseTask.WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken)
                        .ConfigureAwait(false);
                    Console.WriteLine($"Dispatch response: {response.GetType().Name}.");

                    switch (response)
                    {
                        case FileTransferComplete complete when complete.Id == transferId:
                            Console.WriteLine($"File upload complete: {ValueOrDash(complete.FilePath)}.");
                            return;

                        case FileTransferCancel cancel when cancel.Id == transferId:
                            throw new InvalidOperationException($"File upload canceled by client: {ValueOrDash(cancel.Reason)}");

                        default:
                            responseTask = responseSink.WaitForNextAsync(clientId);
                            break;
                    }
                }
            }
            catch
            {
                responseSink.CancelWait(clientId);
                throw;
            }
        }

        private static bool IsUploadFileCommand(string dispatchCommand)
        {
            return string.Equals(dispatchCommand, "upload-file", StringComparison.OrdinalIgnoreCase);
        }

        private static FileType ParsePathType(string pathType)
        {
            if (string.Equals(pathType, "file", StringComparison.OrdinalIgnoreCase))
                return FileType.File;
            if (string.Equals(pathType, "directory", StringComparison.OrdinalIgnoreCase))
                return FileType.Directory;

            throw new ArgumentException("--type must be file or directory.");
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
            Console.WriteLine("Commands: clients | dispatch <client-id|first> <command> [--path <path>] [--new-path <path>] [--type <file|directory>] [--pid <pid>] [--local-address <ip>] [--local-port <port>] [--remote-address <ip>] [--remote-port <port>] [--remote-path <client-path>] [--output <local-path>] | help | exit");
        }

        private static async Task ReceiveAndPrintResponseAsync(
            CliOptions options,
            AwaitableMessageSink responseSink,
            string clientId,
            IMessage command,
            string outputPath,
            CancellationToken cancellationToken)
        {
            await ReceiveAndPrintResponseAsync(
                options,
                responseSink,
                clientId,
                command,
                outputPath,
                responseSink.WaitForNextAsync(clientId),
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task ReceiveAndPrintResponseAsync(
            CliOptions options,
            AwaitableMessageSink responseSink,
            string clientId,
            IMessage command,
            string outputPath,
            Task<IMessage> firstResponseTask,
            CancellationToken cancellationToken)
        {
            if (command is FileTransferRequest request)
            {
                await ReceiveFileTransferAsync(
                    options,
                    responseSink,
                    clientId,
                    request,
                    outputPath,
                    firstResponseTask,
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            IMessage response;
            try
            {
                response = await firstResponseTask.WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken)
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

        private static async Task ReceiveFileTransferAsync(
            CliOptions options,
            AwaitableMessageSink responseSink,
            string clientId,
            FileTransferRequest request,
            string outputPath,
            Task<IMessage> firstResponseTask,
            CancellationToken cancellationToken)
        {
            string resolvedOutputPath = ResolveDownloadOutputPath(request.RemotePath, outputPath);
            long totalBytes = 0;
            long expectedSize = -1;
            bool completed = false;

            try
            {
                using (FileStream output = File.Open(resolvedOutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Task<IMessage> nextResponseTask = firstResponseTask;
                    while (true)
                    {
                        IMessage response;
                        try
                        {
                            response = await nextResponseTask.WaitAsync(TimeSpan.FromSeconds(options.TimeoutSeconds), cancellationToken)
                                .ConfigureAwait(false);
                        }
                        catch
                        {
                            responseSink.CancelWait(clientId);
                            throw;
                        }

                        Console.WriteLine($"Dispatch response: {response.GetType().Name}.");
                        switch (response)
                        {
                            case FileTransferChunk chunk when chunk.Id == request.Id:
                                if (chunk.Chunk == null || chunk.Chunk.Data == null)
                                    throw new InvalidOperationException("File transfer chunk was empty.");
                                if (chunk.Chunk.Offset != output.Position)
                                    throw new InvalidOperationException("File transfer chunk offset was not contiguous.");

                                expectedSize = chunk.FileSize;
                                await output.WriteAsync(chunk.Chunk.Data, 0, chunk.Chunk.Data.Length, cancellationToken)
                                    .ConfigureAwait(false);
                                totalBytes += chunk.Chunk.Data.Length;
                                Console.WriteLine($"File transfer chunk: Offset={chunk.Chunk.Offset}; Bytes={chunk.Chunk.Data.Length}; Total={totalBytes}/{chunk.FileSize}.");
                                break;

                            case FileTransferComplete complete when complete.Id == request.Id:
                                if (expectedSize >= 0 && totalBytes != expectedSize)
                                    throw new InvalidOperationException("File transfer completed with an unexpected byte count.");

                                completed = true;
                                Console.WriteLine($"File transfer complete: {totalBytes} bytes saved to {resolvedOutputPath}.");
                                return;

                            case FileTransferCancel cancel when cancel.Id == request.Id:
                                throw new InvalidOperationException($"File transfer canceled by client: {ValueOrDash(cancel.Reason)}");

                            default:
                                throw new InvalidOperationException($"Unexpected file transfer response '{response.GetType().Name}'.");
                        }

                        nextResponseTask = responseSink.WaitForNextAsync(clientId);
                    }
                }
            }
            finally
            {
                if (!completed && File.Exists(resolvedOutputPath))
                {
                    try
                    {
                        File.Delete(resolvedOutputPath);
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static string ResolveDownloadOutputPath(string remotePath, string outputPath)
        {
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                string resolved = Path.GetFullPath(outputPath);
                string directory = Path.GetDirectoryName(resolved);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                return resolved;
            }

            string fileName = Path.GetFileName(remotePath);
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = "download.bin";

            string downloadDirectory = Path.Combine(Environment.CurrentDirectory, "downloads");
            Directory.CreateDirectory(downloadDirectory);

            string candidate = Path.Combine(downloadDirectory, fileName);
            if (!File.Exists(candidate))
                return candidate;

            string stem = Path.GetFileNameWithoutExtension(fileName);
            string extension = Path.GetExtension(fileName);
            for (int index = 1; ; index++)
            {
                candidate = Path.Combine(downloadDirectory, $"{stem}-{index}{extension}");
                if (!File.Exists(candidate))
                    return candidate;
            }
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
                        foreach (MasterSplinter.Common.Models.Drive drive in response.Drives)
                            lines.Add($"- {ValueOrDash(drive.DisplayName)} => {ValueOrDash(drive.RootDirectory)}");
                    }
                    break;
                case GetDirectoryResponse response:
                    lines.Add($"Directory path: {ValueOrDash(response.RemotePath)}; Items: {Count(response.Items)}.");
                    if (response.Items != null)
                    {
                        foreach (MasterSplinter.Common.Models.FileSystemEntry item in response.Items)
                            lines.Add($"- {item.EntryType} {ValueOrDash(item.Name)} Size={item.Size}");
                    }
                    break;
                case GetProcessesResponse response:
                    lines.Add($"Processes: {Count(response.Processes)}.");
                    if (response.Processes != null)
                    {
                        foreach (MasterSplinter.Common.Models.Process process in response.Processes)
                            lines.Add($"- PID={process.Id} {ValueOrDash(process.Name)} Title={ValueOrDash(process.MainWindowTitle)}");
                    }
                    break;
                case GetStartupItemsResponse response:
                    lines.Add($"Startup items: {Count(response.StartupItems)}.");
                    if (response.StartupItems != null)
                    {
                        foreach (MasterSplinter.Common.Models.StartupItem item in response.StartupItems)
                            lines.Add($"- {item.Type} {ValueOrDash(item.Name)} => {ValueOrDash(item.Path)}");
                    }
                    break;
                case GetConnectionsResponse response:
                    lines.Add($"TCP connections: {Count(response.Connections)}.");
                    if (response.Connections != null)
                    {
                        foreach (MasterSplinter.Common.Models.TcpConnection connection in response.Connections)
                        {
                            lines.Add(
                                $"- {ValueOrDash(connection.ProcessName)} {ValueOrDash(connection.LocalAddress)}:{connection.LocalPort} -> {ValueOrDash(connection.RemoteAddress)}:{connection.RemotePort} {connection.State}");
                        }
                    }
                    break;
                case GetRegistryKeysResponse response:
                    lines.Add($"Registry key: {ValueOrDash(response.RootKey)}; Matches={Count(response.Matches)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    if (response.Matches != null)
                    {
                        foreach (MasterSplinter.Common.Models.RegSeekerMatch match in response.Matches)
                            lines.Add($"- {ValueOrDash(match.Key)} Values={Count(match.Data)} HasSubKeys={match.HasSubKeys}");
                    }
                    break;
                case SetStatusFileManager response:
                    lines.Add($"File manager status: {ValueOrDash(response.Message)}; SetLastDirectorySeen={response.SetLastDirectorySeen}.");
                    break;
                case DoProcessResponse response:
                    lines.Add($"Process response: Action={response.Action}; Result={response.Result}.");
                    break;
                case FileTransferChunk response:
                    int bytes = response.Chunk == null || response.Chunk.Data == null ? 0 : response.Chunk.Data.Length;
                    lines.Add($"File transfer chunk: Id={response.Id}; Offset={(response.Chunk == null ? 0 : response.Chunk.Offset)}; Bytes={bytes}; FileSize={response.FileSize}; Path={ValueOrDash(response.FilePath)}.");
                    break;
                case FileTransferComplete response:
                    lines.Add($"File transfer complete: Id={response.Id}; Path={ValueOrDash(response.FilePath)}.");
                    break;
                case FileTransferCancel response:
                    lines.Add($"File transfer canceled: Id={response.Id}; Reason={ValueOrDash(response.Reason)}.");
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
            private readonly object _gate = new object();
            private readonly Dictionary<string, TaskCompletionSource<IMessage>> _pending =
                new Dictionary<string, TaskCompletionSource<IMessage>>(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, Queue<IMessage>> _queued =
                new Dictionary<string, Queue<IMessage>>(StringComparer.OrdinalIgnoreCase);

            public Task<IMessage> WaitForNextAsync(string clientId)
            {
                if (string.IsNullOrWhiteSpace(clientId))
                    throw new ArgumentException("Client id is required.", nameof(clientId));

                lock (_gate)
                {
                    Queue<IMessage> queue = GetQueue(clientId);
                    if (queue.Count > 0)
                        return Task.FromResult(queue.Dequeue());

                    if (_pending.ContainsKey(clientId))
                        throw new InvalidOperationException($"A dispatch is already waiting for client '{clientId}'.");

                    var pending = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                    _pending.Add(clientId, pending);
                    return pending.Task;
                }
            }

            public void CancelWait(string clientId)
            {
                TaskCompletionSource<IMessage> pending = null;
                lock (_gate)
                {
                    if (_pending.TryGetValue(clientId, out pending))
                        _pending.Remove(clientId);
                }

                pending?.TrySetCanceled();
            }

            public Task HandleAsync(
                IRemoteClientConnection connection,
                IMessage message,
                CancellationToken cancellationToken)
            {
                string clientId = connection.ClientId ?? string.Empty;
                Console.WriteLine(
                    $"Received {message.GetType().Name} from client {connection.ClientId ?? "-"} on {connection.ConnectionId}.");

                TaskCompletionSource<IMessage> pending = null;
                lock (_gate)
                {
                    if (_pending.TryGetValue(clientId, out pending))
                    {
                        _pending.Remove(clientId);
                    }
                    else
                    {
                        GetQueue(clientId).Enqueue(message);
                    }
                }

                pending?.TrySetResult(message);
                return Task.CompletedTask;
            }

            private Queue<IMessage> GetQueue(string clientId)
            {
                if (!_queued.TryGetValue(clientId, out Queue<IMessage> queue))
                {
                    queue = new Queue<IMessage>();
                    _queued.Add(clientId, queue);
                }

                return queue;
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
