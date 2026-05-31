using Quasar.Common.Messages;

namespace LocationRemote.Server.Core.Handshake
{
    public interface IClientIdentificationValidator
    {
        ClientIdentificationValidationResult Validate(ClientIdentification identification);
    }
}
