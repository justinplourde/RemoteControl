using Quasar.Common.Messages;
using System;

namespace LocationRemote.Client.Core.Identity
{
    public sealed class ClientIdentificationFactory : IClientIdentificationFactory
    {
        public ClientIdentification Create(ClientIdentityOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return new ClientIdentification
            {
                Version = options.Version,
                OperatingSystem = options.OperatingSystem,
                AccountType = options.AccountType,
                Country = options.Country,
                CountryCode = options.CountryCode,
                ImageIndex = options.ImageIndex,
                Id = options.ClientId,
                Username = options.Username,
                PcName = options.MachineName,
                Tag = options.Tag,
                EncryptionKey = options.EncryptionKey,
                Signature = options.Signature,
                ProtocolVersion = options.ProtocolVersion,
                Capabilities = options.Capabilities
            };
        }
    }
}
