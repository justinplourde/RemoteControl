using MasterSplinter.Common.Messages;
using MasterSplinter.Server.Core.Sessions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Listeners
{
    public sealed class ClientStatusMessageSink : IRemoteClientMessageSink
    {
        private readonly IClientStatusRegistry _statusRegistry;
        private readonly IRemoteClientMessageSink _innerSink;

        public ClientStatusMessageSink(
            IClientStatusRegistry statusRegistry,
            IRemoteClientMessageSink innerSink)
        {
            _statusRegistry = statusRegistry ?? throw new ArgumentNullException(nameof(statusRegistry));
            _innerSink = innerSink ?? NoOpRemoteClientMessageSink.Instance;
        }

        public Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            string clientId = connection.ClientId;
            if (string.IsNullOrWhiteSpace(clientId))
                return _innerSink.HandleAsync(connection, message, cancellationToken);

            switch (message)
            {
                case SetStatus status:
                    _statusRegistry.SetStatus(clientId, status.Message);
                    break;
                case SetUserStatus userStatus:
                    _statusRegistry.SetUserStatus(clientId, userStatus.Message);
                    return Task.CompletedTask;
            }

            return _innerSink.HandleAsync(connection, message, cancellationToken);
        }
    }
}
