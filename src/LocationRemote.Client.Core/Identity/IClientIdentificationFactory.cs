using Quasar.Common.Messages;

namespace LocationRemote.Client.Core.Identity
{
    public interface IClientIdentificationFactory
    {
        ClientIdentification Create(ClientIdentityOptions options);
    }
}
