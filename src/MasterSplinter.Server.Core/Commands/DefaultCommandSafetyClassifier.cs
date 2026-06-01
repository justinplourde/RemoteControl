using Quasar.Common.Messages;
using Quasar.Common.Messages.ReverseProxy;
using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class DefaultCommandSafetyClassifier : ICommandSafetyClassifier
    {
        public static readonly DefaultCommandSafetyClassifier Instance = new DefaultCommandSafetyClassifier();

        private DefaultCommandSafetyClassifier()
        {
        }

        public CommandSafetyMetadata Classify(IMessage message)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            switch (message)
            {
                case GetSystemInfo _:
                case GetProcesses _:
                case GetStartupItems _:
                case GetConnections _:
                    return CommandSafetyMetadata.ReadOnly(CommandSafetyClass.ReadOnlyInventory);

                case GetDrives _:
                case GetDirectory _:
                    return CommandSafetyMetadata.ReadOnly(CommandSafetyClass.FileRead);

                case FileTransferRequest _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.FileRead, requiresConsent: false);
                case FileTransferChunk _:
                case FileTransferCancel _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.FileWrite, requiresConsent: false);
                case DoPathDelete _:
                case DoPathRename _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.FileWrite, requiresConsent: false);

                case DoProcessStart _:
                case DoProcessEnd _:
                case DoShellExecute _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.Execution, requiresConsent: true);

                case DoShutdownAction _:
                case DoAskElevate _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.SystemControl, requiresConsent: true);

                case DoClientUninstall _:
                case DoStartupItemAdd _:
                case DoStartupItemRemove _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.Persistence, requiresConsent: true);

                case DoClientDisconnect _:
                case DoClientReconnect _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.ConnectionLifecycle, requiresConsent: false);

                case DoCloseConnection _:
                case ReverseProxyConnect _:
                case ReverseProxyData _:
                case ReverseProxyDisconnect _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.NetworkControl, requiresConsent: false);

                case DoLoadRegistryKey _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.ReadOnlyInventory, requiresConsent: false);
                case DoCreateRegistryKey _:
                case DoDeleteRegistryKey _:
                case DoRenameRegistryKey _:
                case DoCreateRegistryValue _:
                case DoDeleteRegistryValue _:
                case DoRenameRegistryValue _:
                case DoChangeRegistryValue _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.Persistence, requiresConsent: false);

                case GetDesktop _:
                case GetMonitors _:
                case DoWebcamStop _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.RemoteCapture, requiresConsent: true);
                case DoMouseEvent _:
                case DoKeyboardEvent _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.RemoteInput, requiresConsent: true);

                case DoShowMessageBox _:
                case DoVisitWebsite _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.UserInteraction, requiresConsent: true);

                case GetPasswords _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.CredentialAccess, requiresConsent: true);
                case GetKeyloggerLogsDirectory _:
                    return CommandSafetyMetadata.Controlled(CommandSafetyClass.KeystrokeAccess, requiresConsent: true);

                default:
                    return CommandSafetyMetadata.Unknown();
            }
        }
    }
}
