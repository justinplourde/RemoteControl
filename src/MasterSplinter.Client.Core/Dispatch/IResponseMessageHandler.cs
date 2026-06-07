using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Dispatch
{
    public interface IResponseMessageHandler<in TMessage> where TMessage : IMessage
    {
        Task<IMessage> HandleAsync(IClientContext context, TMessage message, CancellationToken cancellationToken);
    }
}
