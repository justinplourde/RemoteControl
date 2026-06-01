namespace MasterSplinter.Server.Core.Authorization
{
    public enum OperatorPermission
    {
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
