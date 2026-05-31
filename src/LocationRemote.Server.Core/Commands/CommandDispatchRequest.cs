using Quasar.Common.Messages;
using System;

namespace LocationRemote.Server.Core.Commands
{
    public sealed class CommandDispatchRequest
    {
        public CommandDispatchRequest(
            Guid correlationId,
            string clientId,
            IMessage message,
            string operatorId,
            string source)
        {
            if (correlationId == Guid.Empty)
                throw new ArgumentException("Correlation id is required.", nameof(correlationId));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            CorrelationId = correlationId;
            ClientId = clientId;
            Message = message;
            OperatorId = operatorId;
            Source = source;
        }

        public Guid CorrelationId { get; }

        public string ClientId { get; }

        public IMessage Message { get; }

        public string OperatorId { get; }

        public string Source { get; }

        public string MessageType => Message.GetType().FullName;

        public static CommandDispatchRequest Create(string clientId, IMessage message)
        {
            return new CommandDispatchRequest(Guid.NewGuid(), clientId, message, null, null);
        }
    }
}
