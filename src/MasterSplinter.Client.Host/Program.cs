using MasterSplinter.Client.Core.Connections;
using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using MasterSplinter.Client.Core.Identity;
using MasterSplinter.Client.Core.Processes;
using MasterSplinter.Client.Core.Registry;
using MasterSplinter.Client.Core.Services;
using MasterSplinter.Client.Core.Startup;
using MasterSplinter.Client.Core.SystemInformation;
using MasterSplinter.Client.Host;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Protocol;
using System;
using System.Threading;

namespace MasterSplinter.Client.Host
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                HostOptions options = HostOptions.Parse(args);
                string accountType = new ClientPrivilegeProvider().GetAccountType();
                var identityOptions = new ClientIdentityOptions(
                    "modern-dev",
                    Environment.OSVersion.VersionString,
                    accountType,
                    "Unknown",
                    "XX",
                    0,
                    options.ClientId,
                    Environment.UserName,
                    Environment.MachineName,
                    options.Tag,
                    options.EncryptionKey,
                    options.Signature,
                    new ProtocolVersion { Major = 1, Minor = 0 },
                    new ClientCapabilities());

                identityOptions.Capabilities.SupportedFeatures.Add("handshake");
                identityOptions.Capabilities.SupportedFeatures.Add("filesystem.directory");
                identityOptions.Capabilities.SupportedFeatures.Add("filesystem.delete");
                identityOptions.Capabilities.SupportedFeatures.Add("filesystem.download");
                identityOptions.Capabilities.SupportedFeatures.Add("filesystem.drives");
                identityOptions.Capabilities.SupportedFeatures.Add("filesystem.rename");
                identityOptions.Capabilities.SupportedFeatures.Add("filesystem.upload");
                identityOptions.Capabilities.SupportedFeatures.Add("message.dispatch");
                identityOptions.Capabilities.SupportedFeatures.Add("messagebox.show");
                identityOptions.Capabilities.SupportedFeatures.Add("client.lifecycle");
                identityOptions.Capabilities.SupportedFeatures.Add("processes.end");
                identityOptions.Capabilities.SupportedFeatures.Add("processes.list");
                identityOptions.Capabilities.SupportedFeatures.Add("processes.start");
                identityOptions.Capabilities.SupportedFeatures.Add("registry.read");
                identityOptions.Capabilities.SupportedFeatures.Add("startup.items");
                identityOptions.Capabilities.SupportedFeatures.Add("system.info");
                identityOptions.Capabilities.SupportedFeatures.Add("tcp.connections");
                identityOptions.Capabilities.SupportedFeatures.Add("system.elevation");
                identityOptions.Capabilities.SupportedFeatures.Add("system.power");

                ClientIdentification identification = new ClientIdentificationFactory().Create(identityOptions);

                Console.WriteLine($"MasterSplinter client host prepared identification for {identification.Id}.");

                if (options.SmokeTest)
                {
                    Console.WriteLine($"Target placeholder: {options.Host}:{options.Port}.");
                    Console.WriteLine("Transport not opened in smoke-test mode.");
                    Console.WriteLine("Smoke test completed.");
                    return 0;
                }

                ClientIdentificationResult result;
                if (options.HandleOneCommand || options.HandleCommands)
                {
                    var fileUploadHandler = new FileTransferUploadHandler(new FileUploadProvider());
                    MessageDispatcher dispatcher = new MessageDispatcher.Builder()
                        .AddHandler(new ResponseMessageHandlerAdapter<GetConnections>(
                            new GetConnectionsHandler(new TcpConnectionProvider())))
                        .AddHandler(new DoClientDisconnectHandler())
                        .AddHandler(new DoClientReconnectHandler())
                        .AddHandler(new ResponseMessageHandlerAdapter<DoShowMessageBox>(
                            new DoShowMessageBoxHandler(new MessageBoxProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoCloseConnection>(
                            new DoCloseConnectionHandler(new TcpConnectionProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<GetDirectory>(
                            new GetDirectoryHandler(new DirectoryProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<GetDrives>(
                            new GetDrivesHandler(new DriveProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoPathDelete>(
                            new DoPathDeleteHandler(new PathDeleteProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoPathRename>(
                            new DoPathRenameHandler(new PathRenameProvider())))
                        .AddHandler(new FileTransferRequestHandler(new FileDownloadProvider()))
                        .AddHandler<FileTransferChunk>(fileUploadHandler)
                        .AddHandler<FileTransferCancel>(fileUploadHandler)
                        .AddHandler(new ResponseMessageHandlerAdapter<DoProcessStart>(
                            new DoProcessStartHandler(new ProcessStartProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoProcessEnd>(
                            new DoProcessEndHandler(new ProcessEndProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoAskElevate>(
                            new DoAskElevateHandler(new ElevationRequestProvider(new ClientPrivilegeProvider()))))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoShutdownAction>(
                            new DoShutdownActionHandler(new ShutdownActionProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<GetProcesses>(
                            new GetProcessesHandler(new ProcessProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<DoLoadRegistryKey>(
                            new DoLoadRegistryKeyHandler(new RegistryKeyProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<GetStartupItems>(
                            new GetStartupItemsHandler(new StartupItemProvider())))
                        .AddHandler(new ResponseMessageHandlerAdapter<GetSystemInfo>(
                            new GetSystemInfoHandler(new SystemInfoProvider())))
                        .Build();

                    var commandClient = new LoopbackTcpCommandClient();
                    result = options.HandleCommands
                        ? commandClient
                            .IdentifyAndHandleCommandsAsync(options.Host, options.Port, identification, dispatcher, CancellationToken.None)
                            .GetAwaiter()
                            .GetResult()
                        : commandClient
                            .IdentifyAndHandleOneCommandAsync(options.Host, options.Port, identification, dispatcher, CancellationToken.None)
                            .GetAwaiter()
                            .GetResult();
                }
                else
                {
                    result = new LoopbackTcpHandshakeClient()
                        .IdentifyAsync(options.Host, options.Port, identification, CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }

                Console.WriteLine($"Handshake result: {result.Result}.");
                return result.Result ? 0 : 2;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }
    }
}
