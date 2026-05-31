using Quasar.Common.Messages;
using System;

namespace LocationRemote.Server.Core.Sessions
{
    public sealed class ClientSessionSnapshot
    {
        public ClientSessionSnapshot(
            string clientId,
            ClientIdentification identification,
            bool isConnected,
            DateTimeOffset connectedAtUtc,
            DateTimeOffset lastSeenUtc)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            ClientId = clientId;
            Identification = identification;
            IsConnected = isConnected;
            ConnectedAtUtc = connectedAtUtc;
            LastSeenUtc = lastSeenUtc;
        }

        public string ClientId { get; }

        public ClientIdentification Identification { get; }

        public bool IsConnected { get; }

        public DateTimeOffset ConnectedAtUtc { get; }

        public DateTimeOffset LastSeenUtc { get; }

        public static ClientSessionSnapshot FromSession(IRemoteClientSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            return new ClientSessionSnapshot(
                session.ClientId,
                session.Identification,
                session.IsConnected,
                session.ConnectedAtUtc,
                session.LastSeenUtc);
        }
    }
}
