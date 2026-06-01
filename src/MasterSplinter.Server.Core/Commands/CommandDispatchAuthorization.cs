namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandDispatchAuthorization
    {
        public static readonly CommandDispatchAuthorization None =
            new CommandDispatchAuthorization(false, false);

        public CommandDispatchAuthorization(bool operatorHasPermission, bool clientConsentGranted)
        {
            OperatorHasPermission = operatorHasPermission;
            ClientConsentGranted = clientConsentGranted;
        }

        public bool OperatorHasPermission { get; }

        public bool ClientConsentGranted { get; }
    }
}
