using System;

namespace MasterSplinter.Server.Core.Auditing
{
    public sealed class ServerAuditEvent
    {
        public ServerAuditEvent(
            DateTimeOffset occurredAtUtc,
            Guid correlationId,
            string clientId,
            string operatorId,
            string source,
            string messageType,
            string safetyClass,
            bool requiresPermission,
            bool requiresConsent,
            string outcome,
            string errorMessage)
        {
            if (correlationId == Guid.Empty)
                throw new ArgumentException("Correlation id is required.", nameof(correlationId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));
            if (string.IsNullOrWhiteSpace(messageType))
                throw new ArgumentException("Message type is required.", nameof(messageType));
            if (string.IsNullOrWhiteSpace(safetyClass))
                throw new ArgumentException("Safety class is required.", nameof(safetyClass));
            if (string.IsNullOrWhiteSpace(outcome))
                throw new ArgumentException("Outcome is required.", nameof(outcome));

            OccurredAtUtc = occurredAtUtc;
            CorrelationId = correlationId;
            ClientId = clientId;
            OperatorId = operatorId;
            Source = source;
            MessageType = messageType;
            SafetyClass = safetyClass;
            RequiresPermission = requiresPermission;
            RequiresConsent = requiresConsent;
            Outcome = outcome;
            ErrorMessage = errorMessage;
        }

        public DateTimeOffset OccurredAtUtc { get; }

        public Guid CorrelationId { get; }

        public string ClientId { get; }

        public string OperatorId { get; }

        public string Source { get; }

        public string MessageType { get; }

        public string SafetyClass { get; }

        public bool RequiresPermission { get; }

        public bool RequiresConsent { get; }

        public string Outcome { get; }

        public string ErrorMessage { get; }
    }
}
