using MasterSplinter.Server.Core.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Authorization
{
    public interface IOperatorPermissionService
    {
        Task<bool> HasPermissionAsync(
            OperatorIdentity operatorIdentity,
            OperatorPermission permission,
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata,
            CancellationToken cancellationToken);
    }
}
