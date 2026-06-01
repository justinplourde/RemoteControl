using MasterSplinter.Server.Core.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Authorization
{
    public interface ICommandAuthorizationService
    {
        Task<CommandDispatchAuthorization> AuthorizeAsync(
            OperatorIdentity operatorIdentity,
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata,
            CancellationToken cancellationToken);
    }
}
