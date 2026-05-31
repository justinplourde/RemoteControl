using System;

namespace LocationRemote.Server.Core.Auditing
{
    public sealed class ServerAuditEvent
    {
        public ServerAuditEvent(
            DateTimeOffset occurredAtUtc,
            string clientId,
            string messageType,
            string outcome,
            string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));
            if (string.IsNullOrWhiteSpace(messageType))
                throw new ArgumentException("Message type is required.", nameof(messageType));
            if (string.IsNullOrWhiteSpace(outcome))
                throw new ArgumentException("Outcome is required.", nameof(outcome));

            OccurredAtUtc = occurredAtUtc;
            ClientId = clientId;
            MessageType = messageType;
            Outcome = outcome;
            ErrorMessage = errorMessage;
        }

        public DateTimeOffset OccurredAtUtc { get; }

        public string ClientId { get; }

        public string MessageType { get; }

        public string Outcome { get; }

        public string ErrorMessage { get; }
    }
}
