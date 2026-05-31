using Quasar.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Client.Core.Dispatch
{
    public interface IMessageDispatcher
    {
        Task<DispatchResult> DispatchAsync(IClientContext context, IMessage message, CancellationToken cancellationToken);
    }
}
