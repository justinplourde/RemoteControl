using Quasar.Common.Messages;

namespace LocationRemote.Server.Core.Handshake
{
    public sealed class LegacyClientIdentificationValidator : IClientIdentificationValidator
    {
        public ClientIdentificationValidationResult Validate(ClientIdentification identification)
        {
            if (identification == null)
                return ClientIdentificationValidationResult.Reject("Client identification is required.");

            if (identification.Id == null || identification.Id.Length != 64)
                return ClientIdentificationValidationResult.Reject("Client id must be 64 characters.");

            return ClientIdentificationValidationResult.Accept();
        }
    }
}
