using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Commands
{
    public interface IServerCommandDispatcher
    {
        Task<CommandDispatchResult> DispatchAsync(string clientId, IMessage message, CancellationToken cancellationToken);

        Task<CommandDispatchResult> DispatchAsync(CommandDispatchRequest request, CancellationToken cancellationToken);
    }
}
