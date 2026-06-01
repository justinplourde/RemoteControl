using Quasar.Common.Messages;

namespace MasterSplinter.Client.Core.Identity
{
    public interface IClientIdentificationFactory
    {
        ClientIdentification Create(ClientIdentityOptions options);
    }
}
