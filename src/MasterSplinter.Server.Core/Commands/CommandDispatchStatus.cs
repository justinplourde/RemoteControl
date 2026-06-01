namespace MasterSplinter.Server.Core.Commands
{
    public enum CommandDispatchStatus
    {
        Sent,
        ClientNotFound,
        PermissionDenied,
        ConsentRequired,
        Faulted
    }
}
