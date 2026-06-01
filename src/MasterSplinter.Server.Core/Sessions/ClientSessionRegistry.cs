using System;
using System.Collections.Generic;

namespace MasterSplinter.Server.Core.Sessions
{
    public sealed class ClientSessionRegistry : IClientSessionRegistry
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, IRemoteClientSession> _sessions =
            new Dictionary<string, IRemoteClientSession>(StringComparer.OrdinalIgnoreCase);

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _sessions.Count;
                }
            }
        }

        public void AddOrUpdate(IRemoteClientSession session)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));
            if (string.IsNullOrWhiteSpace(session.ClientId))
                throw new ArgumentException("Client id is required.", nameof(session));

            lock (_syncRoot)
            {
                _sessions[session.ClientId] = session;
            }
        }

        public bool TryGet(string clientId, out IRemoteClientSession session)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            lock (_syncRoot)
            {
                return _sessions.TryGetValue(clientId, out session);
            }
        }

        public bool Remove(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            lock (_syncRoot)
            {
                return _sessions.Remove(clientId);
            }
        }

        public IReadOnlyList<ClientSessionSnapshot> GetSnapshots()
        {
            lock (_syncRoot)
            {
                var snapshots = new List<ClientSessionSnapshot>(_sessions.Count);
                foreach (IRemoteClientSession session in _sessions.Values)
                {
                    snapshots.Add(ClientSessionSnapshot.FromSession(session));
                }

                return snapshots;
            }
        }
    }
}
