using Quasar.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Commands
{
    public interface IServerCommandDispatcher
    {
        Task<CommandDispatchResult> DispatchAsync(string clientId, IMessage message, CancellationToken cancellationToken);
    }
}
