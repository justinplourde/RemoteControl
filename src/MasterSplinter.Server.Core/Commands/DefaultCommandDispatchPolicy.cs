using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class DefaultCommandDispatchPolicy : ICommandDispatchPolicy
    {
        public static readonly DefaultCommandDispatchPolicy Instance = new DefaultCommandDispatchPolicy();

        private DefaultCommandDispatchPolicy()
        {
        }

        public CommandDispatchPolicyDecision Authorize(
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (safetyMetadata == null)
                throw new ArgumentNullException(nameof(safetyMetadata));

            CommandDispatchAuthorization authorization =
                request.Authorization ?? CommandDispatchAuthorization.None;

            if (safetyMetadata.RequiresPermission && !authorization.OperatorHasPermission)
            {
                return CommandDispatchPolicyDecision.Deny(
                    CommandDispatchStatus.PermissionDenied,
                    "Operator permission is required for this command.");
            }

            if (safetyMetadata.RequiresConsent && !authorization.ClientConsentGranted)
            {
                return CommandDispatchPolicyDecision.Deny(
                    CommandDispatchStatus.ConsentRequired,
                    "Client consent is required for this command.");
            }

            return CommandDispatchPolicyDecision.Allow();
        }
    }
}
