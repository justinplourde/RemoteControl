using MasterSplinter.Common.Enums;
using System;
using System.Collections.Generic;

namespace MasterSplinter.Server.Core.Sessions
{
    public sealed class ClientStatusRegistry : IClientStatusRegistry
    {
        private readonly object _syncRoot = new object();
        private readonly Dictionary<string, ClientStatusSnapshot> _statuses =
            new Dictionary<string, ClientStatusSnapshot>(StringComparer.OrdinalIgnoreCase);

        public void SetStatus(string clientId, string statusMessage)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            lock (_syncRoot)
            {
                UserStatus? existingUserStatus = null;
                if (_statuses.TryGetValue(clientId, out ClientStatusSnapshot existing))
                    existingUserStatus = existing.UserStatus;

                _statuses[clientId] = new ClientStatusSnapshot(statusMessage, existingUserStatus);
            }
        }

        public void SetUserStatus(string clientId, UserStatus userStatus)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            lock (_syncRoot)
            {
                string existingStatus = null;
                if (_statuses.TryGetValue(clientId, out ClientStatusSnapshot existing))
                    existingStatus = existing.StatusMessage;

                _statuses[clientId] = new ClientStatusSnapshot(existingStatus, userStatus);
            }
        }

        public bool TryGet(string clientId, out ClientStatusSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            lock (_syncRoot)
            {
                return _statuses.TryGetValue(clientId, out snapshot);
            }
        }
    }
}
