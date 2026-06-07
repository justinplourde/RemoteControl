using MasterSplinter.Common.Messages;

namespace MasterSplinter.Server.Core.Handshake
{
    public interface IClientIdentificationValidator
    {
        ClientIdentificationValidationResult Validate(ClientIdentification identification);
    }
}
