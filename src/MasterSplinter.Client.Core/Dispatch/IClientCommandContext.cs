using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Dispatch
{
    public interface IClientCommandContext : IClientContext
    {
        Task SendAsync(IMessage message, CancellationToken cancellationToken);
    }
}
