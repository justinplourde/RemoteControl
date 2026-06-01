using Quasar.Common.Messages;
using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandDispatchRequest
    {
        public CommandDispatchRequest(
            Guid correlationId,
            string clientId,
            IMessage message,
            string operatorId,
            string source)
            : this(correlationId, clientId, message, operatorId, source, CommandDispatchAuthorization.None)
        {
        }

        public CommandDispatchRequest(
            Guid correlationId,
            string clientId,
            IMessage message,
            string operatorId,
            string source,
            CommandDispatchAuthorization authorization)
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
            Authorization = authorization ?? CommandDispatchAuthorization.None;
        }

        public Guid CorrelationId { get; }

        public string ClientId { get; }

        public IMessage Message { get; }

        public string OperatorId { get; }

        public string Source { get; }

        public CommandDispatchAuthorization Authorization { get; }

        public string MessageType => Message.GetType().FullName;

        public static CommandDispatchRequest Create(string clientId, IMessage message)
        {
            return new CommandDispatchRequest(Guid.NewGuid(), clientId, message, null, null);
        }

        public CommandDispatchRequest WithAuthorization(CommandDispatchAuthorization authorization)
        {
            return new CommandDispatchRequest(
                CorrelationId,
                ClientId,
                Message,
                OperatorId,
                Source,
                authorization);
        }
    }
}
