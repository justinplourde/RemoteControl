using MasterSplinter.Server.Core.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Authorization
{
    public interface IClientConsentService
    {
        Task<bool> HasConsentAsync(
            string clientId,
            OperatorIdentity operatorIdentity,
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata,
            CancellationToken cancellationToken);
    }
}
