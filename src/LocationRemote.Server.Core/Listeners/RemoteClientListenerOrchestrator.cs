using LocationRemote.Server.Core.Handshake;
using LocationRemote.Server.Core.Lifecycle;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Listeners
{
    public sealed class RemoteClientListenerOrchestrator : IRemoteClientListenerHandler
    {
        private readonly IClientHandshakeCoordinator _handshake;
        private readonly IClientConnectionLifecycleCoordinator _lifecycle;
        private readonly IRemoteClientListener _listener;
        private readonly IRemoteClientMessageSink _messageSink;

        public RemoteClientListenerOrchestrator(
            IRemoteClientListener listener,
            IClientConnectionLifecycleCoordinator lifecycle,
            IClientHandshakeCoordinator handshake)
            : this(listener, lifecycle, handshake, NoOpRemoteClientMessageSink.Instance)
        {
        }

        public RemoteClientListenerOrchestrator(
            IRemoteClientListener listener,
            IClientConnectionLifecycleCoordinator lifecycle,
            IClientHandshakeCoordinator handshake,
            IRemoteClientMessageSink messageSink)
        {
            _listener = listener ?? throw new ArgumentNullException(nameof(listener));
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _handshake = handshake ?? throw new ArgumentNullException(nameof(handshake));
            _messageSink = messageSink ?? throw new ArgumentNullException(nameof(messageSink));
        }

        public Task StartAsync(ServerListenOptions options, CancellationToken cancellationToken)
        {
            return _listener.StartAsync(options, this, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _listener.StopAsync(cancellationToken);
        }

        public Task ClientConnectedAsync(IRemoteClientConnection connection, CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            return _lifecycle.ConnectedAsync(connection.ConnectionId, cancellationToken);
        }

        public async Task MessageReceivedAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            if (!connection.IsIdentified)
            {
                if (message is ClientIdentification identification)
                {
                    connection.SetIdentification(identification);
                    ClientHandshakeResult result = await _handshake.IdentifyAsync(
                        connection.ConnectionId,
                        connection,
                        cancellationToken).ConfigureAwait(false);

                    await connection.SendAsync(result.Response, cancellationToken).ConfigureAwait(false);

                    if (!result.Accepted)
                        await connection.DisconnectAsync(result.RejectionReason, cancellationToken).ConfigureAwait(false);

                    return;
                }

                await connection.DisconnectAsync("Client must identify before sending messages.", cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            await _messageSink.HandleAsync(connection, message, cancellationToken).ConfigureAwait(false);
        }

        public Task ClientDisconnectedAsync(
            IRemoteClientConnection connection,
            string reason,
            CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));

            return _lifecycle.DisconnectedAsync(connection.ConnectionId, connection.ClientId, reason, cancellationToken);
        }

        public Task ClientFaultedAsync(
            IRemoteClientConnection connection,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            return _lifecycle.FaultedAsync(connection.ConnectionId, connection.ClientId, exception, cancellationToken);
        }
    }
}
