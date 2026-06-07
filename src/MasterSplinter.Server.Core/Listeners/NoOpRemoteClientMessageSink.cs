using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Listeners
{
    public sealed class NoOpRemoteClientMessageSink : IRemoteClientMessageSink
    {
        public static readonly NoOpRemoteClientMessageSink Instance = new NoOpRemoteClientMessageSink();

        private NoOpRemoteClientMessageSink()
        {
        }

        public Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
