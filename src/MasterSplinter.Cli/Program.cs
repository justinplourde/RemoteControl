using MasterSplinter.Server.Core.Authorization;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.Handshake;
using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.RemoteDesktop;
using MasterSplinter.Server.Core.Sessions;
using MasterSplinter.Server.Host;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Cli
{
#pragma warning disable CA1416
    public static class Program
    {
        private const int FileTransferId = 1;
        private const int UploadChunkSize = 64 * 1024;
        private const long MaxUploadFileSizeBytes = 100L * 1024L * 1024L;
        private const int DefaultDesktopStreamFrames = 30;

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
            var statusRegistry = new ClientStatusRegistry();
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, new CompletionLifecycleSink(null));
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var responseSink = new AwaitableMessageSink();
            var messageSink = new ClientStatusMessageSink(statusRegistry, responseSink);
            var orchestrator = new RemoteClientListenerOrchestrator(
                listener,
                lifecycle,
                handshake,
                messageSink);
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
                        PrintClients(registry, statusRegistry);
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
            var statusRegistry = new ClientStatusRegistry();
            var identified = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var responseSink = new AwaitableMessageSink();
            var messageSink = new ClientStatusMessageSink(statusRegistry, responseSink);
            var lifecycle = new ClientConnectionLifecycleCoordinator(registry, new CompletionLifecycleSink(identified));
            var handshake = new ClientHandshakeCoordinator(lifecycle);
            var listener = new LoopbackTcpRemoteClientListener();
            var orchestrator = new RemoteClientListenerOrchestrator(
                listener,
                lifecycle,
                handshake,
                messageSink);

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

                if (IsDesktopStreamCommand(options.DispatchCommand))
                {
                    await DispatchDesktopStreamAsync(
                        options,
                        new ServerCommandDispatcher(registry),
                        responseSink,
                        session.ClientId,
                        options.OutputPath,
                        options.Quality.GetValueOrDefault(75),
                        options.DisplayIndex.GetValueOrDefault(),
                        options.Frames.GetValueOrDefault(DefaultDesktopStreamFrames),
                        options.FrameDelayMilliseconds.GetValueOrDefault(),
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
                options.Name,
                options.NewName,
                options.Kind,
                options.Data,
                options.ShellCommand,
                options.Quality,
                options.DisplayIndex,
                options.MouseAction,
                options.X,
                options.Y,
                options.MonitorIndex,
                options.Key,
                options.KeyDown,
                options.StartupType,
                options.Pid,
                options.Action,
                options.Caption,
                options.Text,
                options.Button,
                options.Icon,
                options.Url,
                options.Hidden,
                options.LocalAddress,
                options.LocalPort,
                options.RemoteAddress,
                options.RemotePort);
        }

        public static IMessage CreateMessage(string dispatchCommand, string path)
        {
            return CreateMessage(
                dispatchCommand,
                path,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                false,
                null,
                null,
                null,
                null);
        }

        public static IMessage CreateMessage(
            string dispatchCommand,
            string path,
            string newPath,
            string pathType,
            string name,
            string newName,
            string kind,
            string data,
            string shellCommand,
            int? quality,
            int? displayIndex,
            string mouseAction,
            int? x,
            int? y,
            int? monitorIndex,
            byte? key,
            bool? keyDown,
            string startupType,
            int? pid,
            string action,
            string caption,
            string text,
            string button,
            string icon,
            string url,
            bool hidden,
            string localAddress,
            ushort? localPort,
            string remoteAddress,
            ushort? remotePort)
        {
            if (IsUploadFileCommand(dispatchCommand))
                throw new ArgumentException("upload-file is a multi-message command and cannot be created as a single message.");
            if (IsDesktopStreamCommand(dispatchCommand))
                throw new ArgumentException("get-desktop-stream is a multi-message command and cannot be created as a single message.");
            if (string.Equals(dispatchCommand, "get-system-info", StringComparison.OrdinalIgnoreCase))
                return new GetSystemInfo();
            if (string.Equals(dispatchCommand, "get-drives", StringComparison.OrdinalIgnoreCase))
                return new GetDrives();
            if (string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase))
                return new GetDirectory { RemotePath = path };
            if (string.Equals(dispatchCommand, "get-registry-key", StringComparison.OrdinalIgnoreCase))
                return new DoLoadRegistryKey { RootKeyName = path };
            if (string.Equals(dispatchCommand, "registry-create-key", StringComparison.OrdinalIgnoreCase))
                return new DoCreateRegistryKey { ParentPath = path };
            if (string.Equals(dispatchCommand, "registry-delete-key", StringComparison.OrdinalIgnoreCase))
                return new DoDeleteRegistryKey { ParentPath = path, KeyName = name };
            if (string.Equals(dispatchCommand, "registry-rename-key", StringComparison.OrdinalIgnoreCase))
                return new DoRenameRegistryKey { ParentPath = path, OldKeyName = name, NewKeyName = newName };
            if (string.Equals(dispatchCommand, "registry-create-value", StringComparison.OrdinalIgnoreCase))
                return new DoCreateRegistryValue { KeyPath = path, Kind = ParseRegistryValueKind(kind) };
            if (string.Equals(dispatchCommand, "registry-delete-value", StringComparison.OrdinalIgnoreCase))
                return new DoDeleteRegistryValue { KeyPath = path, ValueName = name };
            if (string.Equals(dispatchCommand, "registry-rename-value", StringComparison.OrdinalIgnoreCase))
                return new DoRenameRegistryValue { KeyPath = path, OldValueName = name, NewValueName = newName };
            if (string.Equals(dispatchCommand, "registry-change-value", StringComparison.OrdinalIgnoreCase))
            {
                RegistryValueKind valueKind = ParseRegistryValueKind(kind);
                return new DoChangeRegistryValue
                {
                    KeyPath = path,
                    Value = new RegValueData
                    {
                        Name = name,
                        Kind = valueKind,
                        Data = ParseRegistryValueData(valueKind, data)
                    }
                };
            }
            if (string.Equals(dispatchCommand, "shell-execute", StringComparison.OrdinalIgnoreCase))
                return new DoShellExecute { Command = shellCommand };
            if (string.Equals(dispatchCommand, "get-desktop", StringComparison.OrdinalIgnoreCase))
                return new GetDesktop
                {
                    CreateNew = true,
                    Quality = quality.GetValueOrDefault(75),
                    DisplayIndex = displayIndex.GetValueOrDefault()
                };
            if (string.Equals(dispatchCommand, "mouse-event", StringComparison.OrdinalIgnoreCase))
            {
                MouseAction parsedAction = ParseMouseAction(mouseAction);
                return new DoMouseEvent
                {
                    Action = parsedAction,
                    IsMouseDown = IsMouseDown(parsedAction),
                    X = x.GetValueOrDefault(),
                    Y = y.GetValueOrDefault(),
                    MonitorIndex = monitorIndex.GetValueOrDefault()
                };
            }
            if (string.Equals(dispatchCommand, "keyboard-event", StringComparison.OrdinalIgnoreCase))
                return new DoKeyboardEvent
                {
                    Key = key.GetValueOrDefault(),
                    KeyDown = keyDown.GetValueOrDefault()
                };
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
            if (string.Equals(dispatchCommand, "startup-add", StringComparison.OrdinalIgnoreCase))
                return new DoStartupItemAdd
                {
                    StartupItem = new MasterSplinter.Common.Models.StartupItem
                    {
                        Name = name,
                        Path = path,
                        Type = ParseStartupType(startupType)
                    }
                };
            if (string.Equals(dispatchCommand, "startup-remove", StringComparison.OrdinalIgnoreCase))
                return new DoStartupItemRemove
                {
                    StartupItem = new MasterSplinter.Common.Models.StartupItem
                    {
                        Name = name,
                        Type = ParseStartupType(startupType)
                    }
                };
            if (string.Equals(dispatchCommand, "start-process", StringComparison.OrdinalIgnoreCase))
                return new DoProcessStart { FilePath = path };
            if (string.Equals(dispatchCommand, "end-process", StringComparison.OrdinalIgnoreCase))
                return new DoProcessEnd { Pid = pid.GetValueOrDefault() };
            if (string.Equals(dispatchCommand, "ask-elevate", StringComparison.OrdinalIgnoreCase))
                return new DoAskElevate();
            if (string.Equals(dispatchCommand, "shutdown-action", StringComparison.OrdinalIgnoreCase))
                return new DoShutdownAction { Action = ParseShutdownAction(action) };
            if (string.Equals(dispatchCommand, "disconnect-client", StringComparison.OrdinalIgnoreCase))
                return new DoClientDisconnect();
            if (string.Equals(dispatchCommand, "reconnect-client", StringComparison.OrdinalIgnoreCase))
                return new DoClientReconnect();
            if (string.Equals(dispatchCommand, "uninstall-client", StringComparison.OrdinalIgnoreCase))
                return new DoClientUninstall();
            if (string.Equals(dispatchCommand, "show-message", StringComparison.OrdinalIgnoreCase))
                return new DoShowMessageBox
                {
                    Caption = caption ?? string.Empty,
                    Text = text,
                    Button = ParseMessageBoxButton(button),
                    Icon = ParseMessageBoxIcon(icon)
                };
            if (string.Equals(dispatchCommand, "visit-website", StringComparison.OrdinalIgnoreCase))
                return new DoVisitWebsite
                {
                    Url = NormalizeWebsiteUrl(url),
                    Hidden = hidden
                };
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
            if (string.Equals(dispatchCommand, "get-monitors", StringComparison.OrdinalIgnoreCase))
                return new GetMonitors();

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

            if (IsDesktopStreamCommand(listenCommand.DispatchCommand))
            {
                await DispatchDesktopStreamAsync(
                    options,
                    dispatcher,
                    responseSink,
                    clientId,
                    listenCommand.OutputPath ?? options.OutputPath,
                    listenCommand.Quality.GetValueOrDefault(options.Quality.GetValueOrDefault(75)),
                    listenCommand.DisplayIndex.GetValueOrDefault(options.DisplayIndex.GetValueOrDefault()),
                    listenCommand.Frames.GetValueOrDefault(options.Frames.GetValueOrDefault(DefaultDesktopStreamFrames)),
                    listenCommand.FrameDelayMilliseconds.GetValueOrDefault(options.FrameDelayMilliseconds.GetValueOrDefault()),
                    cancellationToken).ConfigureAwait(false);
                return;
            }

            IMessage message = CreateMessage(
                listenCommand.DispatchCommand,
                listenCommand.Path,
                listenCommand.NewPath,
                listenCommand.PathType,
                listenCommand.Name,
                listenCommand.NewName,
                listenCommand.Kind,
                listenCommand.Data,
                listenCommand.ShellCommand,
                listenCommand.Quality,
                listenCommand.DisplayIndex,
                listenCommand.MouseAction,
                listenCommand.X,
                listenCommand.Y,
                listenCommand.MonitorIndex,
                listenCommand.Key,
                listenCommand.KeyDown,
                listenCommand.StartupType,
                listenCommand.Pid,
                listenCommand.Action,
                listenCommand.Caption,
                listenCommand.Text,
                listenCommand.Button,
                listenCommand.Icon,
                listenCommand.Url,
                listenCommand.Hidden,
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

        private static async Task DispatchDesktopStreamAsync(
            CliOptions options,
            ServerCommandDispatcher dispatcher,
            AwaitableMessageSink responseSink,
            string clientId,
            string outputPath,
            int quality,
            int displayIndex,
            int frames,
            int frameDelayMilliseconds,
            CancellationToken cancellationToken)
        {
            if (frames < 1)
                throw new ArgumentOutOfRangeException(nameof(frames), "Frame count must be one or greater.");
            if (frameDelayMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(frameDelayMilliseconds), "Frame delay must be zero or greater.");

            string outputDirectory = ResolveDesktopStreamOutputDirectory(outputPath);
            Console.WriteLine(
                $"Desktop stream started: Frames={frames}; Quality={quality}; DisplayIndex={displayIndex}; Output={outputDirectory}.");

            var streamOptions = new RemoteDesktopStreamOptions(
                clientId,
                quality,
                displayIndex,
                frames,
                frameDelayMilliseconds,
                TimeSpan.FromSeconds(options.TimeoutSeconds));
            var session = new RemoteDesktopStreamSession(
                dispatcher,
                responseSink,
                (message, token) => CreateAuthorizedRequestAsync(options, clientId, message, token));

            RemoteDesktopStreamResult result = await session.RunAsync(
                streamOptions,
                (frame, token) =>
                {
                    GetDesktopResponse desktopResponse = frame.Response;
                    string savedPath = SaveDesktopStreamFrame(desktopResponse, outputDirectory, frame.FrameNumber);
                    Console.WriteLine(
                        $"Desktop stream frame {frame.FrameNumber}/{frames}: Monitor={desktopResponse.Monitor}; Quality={desktopResponse.Quality}; Resolution={(desktopResponse.Resolution == null ? "-" : desktopResponse.Resolution.ToString())}; Saved={ValueOrDash(savedPath)}.");

                    return Task.CompletedTask;
                },
                cancellationToken).ConfigureAwait(false);

            if (result.LastDispatchResult != null && result.LastDispatchResult.Status != CommandDispatchStatus.Sent)
            {
                CommandDispatchResult dispatchResult = result.LastDispatchResult;
                Console.WriteLine(
                    $"Desktop stream dispatch stopped: {dispatchResult.Status}. Safety={dispatchResult.SafetyMetadata.SafetyClass}; RequiresPermission={dispatchResult.SafetyMetadata.RequiresPermission}; RequiresConsent={dispatchResult.SafetyMetadata.RequiresConsent}.");
            }

            Console.WriteLine("Desktop stream stopped.");
        }

        private static bool IsUploadFileCommand(string dispatchCommand)
        {
            return string.Equals(dispatchCommand, "upload-file", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDesktopStreamCommand(string dispatchCommand)
        {
            return string.Equals(dispatchCommand, "get-desktop-stream", StringComparison.OrdinalIgnoreCase);
        }

        private static FileType ParsePathType(string pathType)
        {
            if (string.Equals(pathType, "file", StringComparison.OrdinalIgnoreCase))
                return FileType.File;
            if (string.Equals(pathType, "directory", StringComparison.OrdinalIgnoreCase))
                return FileType.Directory;

            throw new ArgumentException("--type must be file or directory.");
        }

        private static StartupType ParseStartupType(string startupType)
        {
            if (string.Equals(startupType, "local-machine-run", StringComparison.OrdinalIgnoreCase))
                return StartupType.LocalMachineRun;
            if (string.Equals(startupType, "local-machine-run-once", StringComparison.OrdinalIgnoreCase))
                return StartupType.LocalMachineRunOnce;
            if (string.Equals(startupType, "current-user-run", StringComparison.OrdinalIgnoreCase))
                return StartupType.CurrentUserRun;
            if (string.Equals(startupType, "current-user-run-once", StringComparison.OrdinalIgnoreCase))
                return StartupType.CurrentUserRunOnce;
            if (string.Equals(startupType, "start-menu", StringComparison.OrdinalIgnoreCase))
                return StartupType.StartMenu;
            if (string.Equals(startupType, "local-machine-run-x86", StringComparison.OrdinalIgnoreCase))
                return StartupType.LocalMachineRunX86;
            if (string.Equals(startupType, "local-machine-run-once-x86", StringComparison.OrdinalIgnoreCase))
                return StartupType.LocalMachineRunOnceX86;

            throw new ArgumentException("--startup-type must be local-machine-run, local-machine-run-once, current-user-run, current-user-run-once, start-menu, local-machine-run-x86, or local-machine-run-once-x86.");
        }

        private static MouseAction ParseMouseAction(string mouseAction)
        {
            if (string.Equals(mouseAction, "left-down", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.LeftDown), StringComparison.OrdinalIgnoreCase))
                return MouseAction.LeftDown;
            if (string.Equals(mouseAction, "left-up", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.LeftUp), StringComparison.OrdinalIgnoreCase))
                return MouseAction.LeftUp;
            if (string.Equals(mouseAction, "right-down", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.RightDown), StringComparison.OrdinalIgnoreCase))
                return MouseAction.RightDown;
            if (string.Equals(mouseAction, "right-up", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.RightUp), StringComparison.OrdinalIgnoreCase))
                return MouseAction.RightUp;
            if (string.Equals(mouseAction, "move", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, "move-cursor", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.MoveCursor), StringComparison.OrdinalIgnoreCase))
                return MouseAction.MoveCursor;
            if (string.Equals(mouseAction, "scroll-up", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.ScrollUp), StringComparison.OrdinalIgnoreCase))
                return MouseAction.ScrollUp;
            if (string.Equals(mouseAction, "scroll-down", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.ScrollDown), StringComparison.OrdinalIgnoreCase))
                return MouseAction.ScrollDown;
            if (string.Equals(mouseAction, "none", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mouseAction, nameof(MouseAction.None), StringComparison.OrdinalIgnoreCase))
                return MouseAction.None;

            throw new ArgumentException("--mouse-action must be left-down, left-up, right-down, right-up, move, scroll-up, scroll-down, or none.");
        }

        private static bool IsMouseDown(MouseAction action)
        {
            return action == MouseAction.LeftDown || action == MouseAction.RightDown;
        }

        private static RegistryValueKind ParseRegistryValueKind(string kind)
        {
            if (string.Equals(kind, "string", StringComparison.OrdinalIgnoreCase))
                return RegistryValueKind.String;
            if (string.Equals(kind, "expand-string", StringComparison.OrdinalIgnoreCase))
                return RegistryValueKind.ExpandString;
            if (string.Equals(kind, "binary", StringComparison.OrdinalIgnoreCase))
                return RegistryValueKind.Binary;
            if (string.Equals(kind, "dword", StringComparison.OrdinalIgnoreCase))
                return RegistryValueKind.DWord;
            if (string.Equals(kind, "qword", StringComparison.OrdinalIgnoreCase))
                return RegistryValueKind.QWord;
            if (string.Equals(kind, "multi-string", StringComparison.OrdinalIgnoreCase))
                return RegistryValueKind.MultiString;

            throw new ArgumentException("--kind must be string, expand-string, binary, dword, qword, or multi-string.");
        }

        private static byte[] ParseRegistryValueData(RegistryValueKind kind, string data)
        {
            switch (kind)
            {
                case RegistryValueKind.Binary:
                    return ParseHexBytes(data);
                case RegistryValueKind.MultiString:
                    return GetStringArrayBytes((data ?? string.Empty).Split(new[] { '|' }, StringSplitOptions.None));
                case RegistryValueKind.DWord:
                    return BitConverter.GetBytes(uint.Parse(data, CultureInfo.InvariantCulture));
                case RegistryValueKind.QWord:
                    return BitConverter.GetBytes(ulong.Parse(data, CultureInfo.InvariantCulture));
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return GetStringBytes(data ?? string.Empty);
                default:
                    return new byte[0];
            }
        }

        private static byte[] ParseHexBytes(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return new byte[0];

            string normalized = data
                .Replace("-", string.Empty)
                .Replace(":", string.Empty)
                .Replace(" ", string.Empty);
            if (normalized.Length % 2 != 0)
                throw new ArgumentException("--data for binary values must contain an even number of hex digits.");

            byte[] bytes = new byte[normalized.Length / 2];
            for (int index = 0; index < bytes.Length; index++)
                bytes[index] = byte.Parse(normalized.Substring(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return bytes;
        }

        private static byte[] GetStringBytes(string value)
        {
            return Encoding.Unicode.GetBytes(value ?? string.Empty);
        }

        private static byte[] GetStringArrayBytes(string[] values)
        {
            var bytes = new List<byte>();
            foreach (string value in values ?? Array.Empty<string>())
            {
                bytes.AddRange(GetStringBytes(value));
                bytes.Add(0);
                bytes.Add(0);
            }

            return bytes.ToArray();
        }

        private static ShutdownAction ParseShutdownAction(string action)
        {
            if (string.Equals(action, "shutdown", StringComparison.OrdinalIgnoreCase))
                return ShutdownAction.Shutdown;
            if (string.Equals(action, "restart", StringComparison.OrdinalIgnoreCase))
                return ShutdownAction.Restart;
            if (string.Equals(action, "standby", StringComparison.OrdinalIgnoreCase))
                return ShutdownAction.Standby;

            throw new ArgumentException("--action must be shutdown, restart, or standby.");
        }

        private static string ParseMessageBoxButton(string button)
        {
            if (string.IsNullOrWhiteSpace(button))
                return "OK";
            if (string.Equals(button, "AbortRetryIgnore", StringComparison.OrdinalIgnoreCase))
                return "AbortRetryIgnore";
            if (string.Equals(button, "OK", StringComparison.OrdinalIgnoreCase))
                return "OK";
            if (string.Equals(button, "OKCancel", StringComparison.OrdinalIgnoreCase))
                return "OKCancel";
            if (string.Equals(button, "RetryCancel", StringComparison.OrdinalIgnoreCase))
                return "RetryCancel";
            if (string.Equals(button, "YesNo", StringComparison.OrdinalIgnoreCase))
                return "YesNo";
            if (string.Equals(button, "YesNoCancel", StringComparison.OrdinalIgnoreCase))
                return "YesNoCancel";

            throw new ArgumentException("--button must be AbortRetryIgnore, OK, OKCancel, RetryCancel, YesNo, or YesNoCancel.");
        }

        private static string ParseMessageBoxIcon(string icon)
        {
            if (string.IsNullOrWhiteSpace(icon))
                return "None";
            if (string.Equals(icon, "None", StringComparison.OrdinalIgnoreCase))
                return "None";
            if (string.Equals(icon, "Error", StringComparison.OrdinalIgnoreCase))
                return "Error";
            if (string.Equals(icon, "Hand", StringComparison.OrdinalIgnoreCase))
                return "Hand";
            if (string.Equals(icon, "Question", StringComparison.OrdinalIgnoreCase))
                return "Question";
            if (string.Equals(icon, "Exclamation", StringComparison.OrdinalIgnoreCase))
                return "Exclamation";
            if (string.Equals(icon, "Warning", StringComparison.OrdinalIgnoreCase))
                return "Warning";
            if (string.Equals(icon, "Information", StringComparison.OrdinalIgnoreCase))
                return "Information";
            if (string.Equals(icon, "Asterisk", StringComparison.OrdinalIgnoreCase))
                return "Asterisk";

            throw new ArgumentException("--icon must be None, Error, Hand, Question, Exclamation, Warning, Information, or Asterisk.");
        }

        private static string NormalizeWebsiteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("--url is required for visit-website.");

            if (url.Contains("://") &&
                !url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("--url must be an HTTP or HTTPS URL.");
            }

            string normalized = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? url
                : "http://" + url;

            if (!Uri.TryCreate(normalized, UriKind.Absolute, out Uri uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException("--url must be an HTTP or HTTPS URL.");
            }

            return uri.AbsoluteUri;
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

        private static void PrintClients(ClientSessionRegistry registry, IClientStatusRegistry statusRegistry)
        {
            IReadOnlyList<ClientSessionSnapshot> snapshots = registry.GetSnapshots();
            Console.WriteLine($"Clients: {snapshots.Count}.");
            foreach (ClientSessionSnapshot snapshot in snapshots)
            {
                string user = snapshot.Identification == null ? "-" : ValueOrDash(snapshot.Identification.Username);
                string machine = snapshot.Identification == null ? "-" : ValueOrDash(snapshot.Identification.PcName);
                string accountType = snapshot.Identification == null ? "-" : ValueOrDash(snapshot.Identification.AccountType);
                string status = "-";
                string userStatus = "-";
                if (statusRegistry != null && statusRegistry.TryGet(snapshot.ClientId, out ClientStatusSnapshot statusSnapshot))
                {
                    status = ValueOrDash(statusSnapshot.StatusMessage);
                    userStatus = statusSnapshot.UserStatus.HasValue
                        ? statusSnapshot.UserStatus.Value.ToString()
                        : "-";
                }

                Console.WriteLine($"- {snapshot.ClientId} Connected={snapshot.IsConnected} User={user} Machine={machine} AccountType={accountType} Status={status} UserStatus={userStatus}");
            }
        }

        private static void PrintListenHelp()
        {
            Console.WriteLine("Commands: clients | dispatch <client-id|first> <command> [--path <path>] [--new-path <path>] [--type <file|directory>] [--name <name>] [--new-name <name>] [--kind <registry-kind>] [--data <value>] [--shell-command <command>] [--quality <1-100>] [--display-index <index>] [--frames <count>] [--frame-delay-ms <milliseconds>] [--startup-type <type>] [--pid <pid>] [--action <shutdown|restart|standby>] [--caption <title>] [--text <message>] [--button <button>] [--icon <icon>] [--url <http-url>] [--hidden] [--local-address <ip>] [--local-port <port>] [--remote-address <ip>] [--remote-port <port>] [--remote-path <client-path>] [--output <local-path-or-directory>] | help | exit");
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
            if (command is GetDesktop && response is GetDesktopResponse desktopResponse)
                SaveDesktopFrame(desktopResponse, outputPath);

            PrintResponse(response);
        }

        private static void SaveDesktopFrame(GetDesktopResponse response, string outputPath)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (response.Image == null || response.Image.Length == 0)
            {
                Console.WriteLine("Desktop frame was empty; no image was saved.");
                return;
            }

            byte[] image = ExtractLegacyFirstFrameJpeg(response.Image);
            string resolvedOutputPath = ResolveDesktopOutputPath(response.Monitor, outputPath);
            File.WriteAllBytes(resolvedOutputPath, image);
            Console.WriteLine($"Desktop frame saved: {image.Length} bytes to {resolvedOutputPath}.");
        }

        private static string SaveDesktopStreamFrame(
            GetDesktopResponse response,
            string outputDirectory,
            int frameNumber)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
            if (frameNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(frameNumber), "Frame number must be one or greater.");
            if (response.Image == null || response.Image.Length == 0)
                return null;

            byte[] image = ExtractLegacyFirstFrameJpeg(response.Image);
            string path = Path.Combine(outputDirectory, $"desktop-monitor{response.Monitor}-frame{frameNumber:0000}.jpg");
            File.WriteAllBytes(path, image);
            return path;
        }

        private static byte[] ExtractLegacyFirstFrameJpeg(byte[] image)
        {
            if (image.Length < 4)
                return image;

            int length = BitConverter.ToInt32(image, 0);
            if (length <= 0 || length > image.Length - 4)
                return image;

            var jpeg = new byte[length];
            Buffer.BlockCopy(image, 4, jpeg, 0, length);
            return jpeg;
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

        private static string ResolveDesktopOutputPath(int monitor, string outputPath)
        {
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                string resolved = Path.GetFullPath(outputPath);
                string directory = Path.GetDirectoryName(resolved);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                return resolved;
            }

            string captureDirectory = Path.Combine(Environment.CurrentDirectory, "captures");
            Directory.CreateDirectory(captureDirectory);

            string candidate = Path.Combine(captureDirectory, $"desktop-monitor{monitor}.jpg");
            if (!File.Exists(candidate))
                return candidate;

            for (int index = 1; ; index++)
            {
                candidate = Path.Combine(captureDirectory, $"desktop-monitor{monitor}-{index}.jpg");
                if (!File.Exists(candidate))
                    return candidate;
            }
        }

        private static string ResolveDesktopStreamOutputDirectory(string outputPath)
        {
            string directory = string.IsNullOrWhiteSpace(outputPath)
                ? Path.Combine(Environment.CurrentDirectory, "captures", $"desktop-stream-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}")
                : Path.GetFullPath(outputPath);

            Directory.CreateDirectory(directory);
            return directory;
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
                case GetMonitorsResponse response:
                    lines.Add($"Monitors: {response.Number}.");
                    break;
                case GetDesktopResponse response:
                    int imageBytes = response.Image == null ? 0 : response.Image.Length;
                    lines.Add($"Desktop frame: Monitor={response.Monitor}; Quality={response.Quality}; Resolution={(response.Resolution == null ? "-" : response.Resolution.ToString())}; ImageBytes={imageBytes}.");
                    break;
                case GetRegistryKeysResponse response:
                    lines.Add($"Registry key: {ValueOrDash(response.RootKey)}; Matches={Count(response.Matches)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    if (response.Matches != null)
                    {
                        foreach (MasterSplinter.Common.Models.RegSeekerMatch match in response.Matches)
                            lines.Add($"- {ValueOrDash(match.Key)} Values={Count(match.Data)} HasSubKeys={match.HasSubKeys}");
                    }
                    break;
                case GetCreateRegistryKeyResponse response:
                    lines.Add($"Registry create key: Parent={ValueOrDash(response.ParentPath)}; Key={ValueOrDash(response.Match == null ? null : response.Match.Key)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case GetDeleteRegistryKeyResponse response:
                    lines.Add($"Registry delete key: Parent={ValueOrDash(response.ParentPath)}; Key={ValueOrDash(response.KeyName)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case GetRenameRegistryKeyResponse response:
                    lines.Add($"Registry rename key: Parent={ValueOrDash(response.ParentPath)}; OldKey={ValueOrDash(response.OldKeyName)}; NewKey={ValueOrDash(response.NewKeyName)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case GetCreateRegistryValueResponse response:
                    lines.Add($"Registry create value: Key={ValueOrDash(response.KeyPath)}; Value={ValueOrDash(response.Value == null ? null : response.Value.Name)}; Kind={(response.Value == null ? RegistryValueKind.Unknown : response.Value.Kind)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case GetDeleteRegistryValueResponse response:
                    lines.Add($"Registry delete value: Key={ValueOrDash(response.KeyPath)}; Value={ValueOrDash(response.ValueName)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case GetRenameRegistryValueResponse response:
                    lines.Add($"Registry rename value: Key={ValueOrDash(response.KeyPath)}; OldValue={ValueOrDash(response.OldValueName)}; NewValue={ValueOrDash(response.NewValueName)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case GetChangeRegistryValueResponse response:
                    lines.Add($"Registry change value: Key={ValueOrDash(response.KeyPath)}; Value={ValueOrDash(response.Value == null ? null : response.Value.Name)}; Kind={(response.Value == null ? RegistryValueKind.Unknown : response.Value.Kind)}; IsError={response.IsError}; Error={ValueOrDash(response.ErrorMsg)}.");
                    break;
                case DoShellExecuteResponse response:
                    lines.Add($"Shell response: IsError={response.IsError}; Output={ValueOrDash(response.Output)}");
                    break;
                case SetStatusFileManager response:
                    lines.Add($"File manager status: {ValueOrDash(response.Message)}; SetLastDirectorySeen={response.SetLastDirectorySeen}.");
                    break;
                case SetStatus response:
                    lines.Add($"Status: {ValueOrDash(response.Message)}");
                    break;
                case SetUserStatus response:
                    lines.Add($"User status: {response.Message}");
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

        private sealed class AwaitableMessageSink : IRemoteClientMessageSink, IRemoteClientResponseSource
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

            public async Task<IMessage> WaitForNextAsync(
                string clientId,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                return await WaitForNextAsync(clientId)
                    .WaitAsync(timeout, cancellationToken)
                    .ConfigureAwait(false);
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
#pragma warning restore CA1416
}
