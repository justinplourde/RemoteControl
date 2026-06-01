namespace MasterSplinter.Server.Core.Commands
{
    public enum CommandSafetyClass
    {
        Unknown,
        ReadOnlyInventory,
        FileRead,
        FileWrite,
        Execution,
        SystemControl,
        RemoteCapture,
        RemoteInput,
        Persistence,
        NetworkControl,
        CredentialAccess,
        KeystrokeAccess,
        UserInteraction,
        ConnectionLifecycle
    }
}
