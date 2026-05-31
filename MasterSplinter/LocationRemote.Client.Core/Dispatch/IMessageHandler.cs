using Quasar.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Client.Core.Dispatch
{
    public interface IMessageHandler<in TMessage> where TMessage : IMessage
    {
        Task HandleAsync(IClientContext context, TMessage message, CancellationToken cancellationToken);
    }
}
