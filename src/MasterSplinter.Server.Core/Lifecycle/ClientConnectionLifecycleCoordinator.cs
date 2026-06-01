using MasterSplinter.Server.Core.Sessions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Lifecycle
{
    public sealed class ClientConnectionLifecycleCoordinator : IClientConnectionLifecycleCoordinator
    {
        private readonly IClientConnectionLifecycleSink _sink;
        private readonly IClientSessionRegistry _sessions;

        public ClientConnectionLifecycleCoordinator(IClientSessionRegistry sessions)
            : this(sessions, NoOpClientConnectionLifecycleSink.Instance)
        {
        }

        public ClientConnectionLifecycleCoordinator(
            IClientSessionRegistry sessions,
            IClientConnectionLifecycleSink sink)
        {
            _sessions = sessions ?? throw new ArgumentNullException(nameof(sessions));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public Task ConnectedAsync(string connectionId, CancellationToken cancellationToken)
        {
            ValidateConnectionId(connectionId);

            return WriteAsync(
                ClientConnectionLifecycleEventKind.Connected,
                connectionId,
                null,
                null,
                null,
                null,
                cancellationToken);
        }

        public Task IdentifiedAsync(string connectionId, IRemoteClientSession session, CancellationToken cancellationToken)
        {
            ValidateConnectionId(connectionId);
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            _sessions.AddOrUpdate(session);

            return WriteAsync(
                ClientConnectionLifecycleEventKind.Identified,
                connectionId,
                session.ClientId,
                session.Identification,
                null,
                null,
                cancellationToken);
        }

        public async Task DisconnectedAsync(string connectionId, string clientId, string reason, CancellationToken cancellationToken)
        {
            ValidateConnectionId(connectionId);

            if (!string.IsNullOrWhiteSpace(clientId))
                _sessions.Remove(clientId);

            await WriteAsync(
                ClientConnectionLifecycleEventKind.Disconnected,
                connectionId,
                clientId,
                null,
                reason,
                null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task FaultedAsync(string connectionId, string clientId, Exception exception, CancellationToken cancellationToken)
        {
            ValidateConnectionId(connectionId);
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            if (!string.IsNullOrWhiteSpace(clientId))
                _sessions.Remove(clientId);

            await WriteAsync(
                ClientConnectionLifecycleEventKind.Faulted,
                connectionId,
                clientId,
                null,
                null,
                exception,
                cancellationToken).ConfigureAwait(false);
        }

        private static void ValidateConnectionId(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("Connection id is required.", nameof(connectionId));
        }

        private Task WriteAsync(
            ClientConnectionLifecycleEventKind kind,
            string connectionId,
            string clientId,
            Quasar.Common.Messages.ClientIdentification identification,
            string reason,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var lifecycleEvent = new ClientConnectionLifecycleEvent(
                kind,
                DateTimeOffset.UtcNow,
                connectionId,
                clientId,
                identification,
                reason,
                exception);

            return _sink.WriteAsync(lifecycleEvent, cancellationToken);
        }
    }
}
