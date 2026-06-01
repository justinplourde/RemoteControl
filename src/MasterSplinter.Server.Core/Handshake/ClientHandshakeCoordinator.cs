using MasterSplinter.Server.Core.Lifecycle;
using MasterSplinter.Server.Core.Sessions;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Handshake
{
    public sealed class ClientHandshakeCoordinator : IClientHandshakeCoordinator
    {
        private readonly IClientConnectionLifecycleCoordinator _lifecycle;
        private readonly IClientIdentificationValidator _validator;

        public ClientHandshakeCoordinator(IClientConnectionLifecycleCoordinator lifecycle)
            : this(lifecycle, new LegacyClientIdentificationValidator())
        {
        }

        public ClientHandshakeCoordinator(
            IClientConnectionLifecycleCoordinator lifecycle,
            IClientIdentificationValidator validator)
        {
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<ClientHandshakeResult> IdentifyAsync(
            string connectionId,
            IRemoteClientSession session,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
                throw new ArgumentException("Connection id is required.", nameof(connectionId));
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            ClientIdentification identification = session.Identification;
            ClientIdentificationValidationResult validation = _validator.Validate(identification);
            var response = new ClientIdentificationResult { Result = validation.Accepted };

            if (!validation.Accepted)
            {
                return new ClientHandshakeResult(
                    false,
                    response,
                    identification == null ? null : identification.Id,
                    identification == null ? null : identification.ProtocolVersion,
                    identification == null ? null : identification.Capabilities,
                    validation.RejectionReason);
            }

            await _lifecycle.IdentifiedAsync(connectionId, session, cancellationToken).ConfigureAwait(false);

            return new ClientHandshakeResult(
                true,
                response,
                identification.Id,
                identification.ProtocolVersion,
                identification.Capabilities,
                null);
        }
    }
}
