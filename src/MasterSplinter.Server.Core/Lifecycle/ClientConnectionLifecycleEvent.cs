using Quasar.Common.Messages;
using System;

namespace MasterSplinter.Server.Core.Lifecycle
{
    public sealed class ClientConnectionLifecycleEvent
    {
        public ClientConnectionLifecycleEvent(
            ClientConnectionLifecycleEventKind kind,
            DateTimeOffset occurredAtUtc,
            string connectionId,
            string clientId,
            ClientIdentification identification,
            string reason,
            Exception exception)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("Connection id is required.", nameof(connectionId));

            Kind = kind;
            OccurredAtUtc = occurredAtUtc;
            ConnectionId = connectionId;
            ClientId = clientId;
            Identification = identification;
            Reason = reason;
            Exception = exception;
        }

        public ClientConnectionLifecycleEventKind Kind { get; }

        public DateTimeOffset OccurredAtUtc { get; }

        public string ConnectionId { get; }

        public string ClientId { get; }

        public ClientIdentification Identification { get; }

        public string Reason { get; }

        public Exception Exception { get; }
    }
}
