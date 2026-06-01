using MasterSplinter.Server.Core.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Authorization
{
    public sealed class CommandAuthorizationService : ICommandAuthorizationService
    {
        private readonly IClientConsentService _consentService;
        private readonly IOperatorPermissionService _permissionService;

        public CommandAuthorizationService(
            IOperatorPermissionService permissionService,
            IClientConsentService consentService)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _consentService = consentService ?? throw new ArgumentNullException(nameof(consentService));
        }

        public async Task<CommandDispatchAuthorization> AuthorizeAsync(
            OperatorIdentity operatorIdentity,
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata,
            CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (safetyMetadata == null)
                throw new ArgumentNullException(nameof(safetyMetadata));

            bool hasPermission = true;
            if (safetyMetadata.RequiresPermission)
            {
                if (operatorIdentity == null)
                    hasPermission = false;
                else
                {
                    OperatorPermission permission = ToPermission(safetyMetadata.SafetyClass);
                    hasPermission = await _permissionService.HasPermissionAsync(
                        operatorIdentity,
                        permission,
                        request,
                        safetyMetadata,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            bool hasConsent = true;
            if (safetyMetadata.RequiresConsent)
            {
                hasConsent = await _consentService.HasConsentAsync(
                    request.ClientId,
                    operatorIdentity,
                    request,
                    safetyMetadata,
                    cancellationToken).ConfigureAwait(false);
            }

            return new CommandDispatchAuthorization(hasPermission, hasConsent);
        }

        public static OperatorPermission ToPermission(CommandSafetyClass safetyClass)
        {
            switch (safetyClass)
            {
                case CommandSafetyClass.ReadOnlyInventory:
                    return OperatorPermission.ReadOnlyInventory;
                case CommandSafetyClass.FileRead:
                    return OperatorPermission.FileRead;
                case CommandSafetyClass.FileWrite:
                    return OperatorPermission.FileWrite;
                case CommandSafetyClass.Execution:
                    return OperatorPermission.Execution;
                case CommandSafetyClass.SystemControl:
                    return OperatorPermission.SystemControl;
                case CommandSafetyClass.RemoteCapture:
                    return OperatorPermission.RemoteCapture;
                case CommandSafetyClass.RemoteInput:
                    return OperatorPermission.RemoteInput;
                case CommandSafetyClass.Persistence:
                    return OperatorPermission.Persistence;
                case CommandSafetyClass.NetworkControl:
                    return OperatorPermission.NetworkControl;
                case CommandSafetyClass.CredentialAccess:
                    return OperatorPermission.CredentialAccess;
                case CommandSafetyClass.KeystrokeAccess:
                    return OperatorPermission.KeystrokeAccess;
                case CommandSafetyClass.UserInteraction:
                    return OperatorPermission.UserInteraction;
                case CommandSafetyClass.ConnectionLifecycle:
                    return OperatorPermission.ConnectionLifecycle;
                default:
                    return OperatorPermission.SystemControl;
            }
        }
    }
}
