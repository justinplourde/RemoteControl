using Quasar.Common.Protocol;
using System;

namespace LocationRemote.Client.Core.Identity
{
    public sealed class ClientIdentityOptions
    {
        public ClientIdentityOptions(
            string version,
            string operatingSystem,
            string accountType,
            string country,
            string countryCode,
            int imageIndex,
            string clientId,
            string username,
            string machineName,
            string tag,
            string encryptionKey,
            byte[] signature,
            ProtocolVersion protocolVersion,
            ClientCapabilities capabilities)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            Version = version;
            OperatingSystem = operatingSystem;
            AccountType = accountType;
            Country = country;
            CountryCode = countryCode;
            ImageIndex = imageIndex;
            ClientId = clientId;
            Username = username;
            MachineName = machineName;
            Tag = tag;
            EncryptionKey = encryptionKey;
            Signature = signature;
            ProtocolVersion = protocolVersion;
            Capabilities = capabilities;
        }

        public string Version { get; }

        public string OperatingSystem { get; }

        public string AccountType { get; }

        public string Country { get; }

        public string CountryCode { get; }

        public int ImageIndex { get; }

        public string ClientId { get; }

        public string Username { get; }

        public string MachineName { get; }

        public string Tag { get; }

        public string EncryptionKey { get; }

        public byte[] Signature { get; }

        public ProtocolVersion ProtocolVersion { get; }

        public ClientCapabilities Capabilities { get; }
    }
}
