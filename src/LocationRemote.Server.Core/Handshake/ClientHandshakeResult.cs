using Quasar.Common.Messages;
using Quasar.Common.Protocol;

namespace LocationRemote.Server.Core.Handshake
{
    public sealed class ClientHandshakeResult
    {
        public ClientHandshakeResult(
            bool accepted,
            ClientIdentificationResult response,
            string clientId,
            ProtocolVersion protocolVersion,
            ClientCapabilities capabilities,
            string rejectionReason)
        {
            Accepted = accepted;
            Response = response;
            ClientId = clientId;
            ProtocolVersion = protocolVersion;
            Capabilities = capabilities;
            RejectionReason = rejectionReason;
        }

        public bool Accepted { get; }

        public ClientIdentificationResult Response { get; }

        public string ClientId { get; }

        public ProtocolVersion ProtocolVersion { get; }

        public ClientCapabilities Capabilities { get; }

        public string RejectionReason { get; }
    }
}
