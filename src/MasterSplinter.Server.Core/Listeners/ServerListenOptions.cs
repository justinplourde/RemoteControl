using System;
using System.Security.Cryptography.X509Certificates;

namespace MasterSplinter.Server.Core.Listeners
{
    public sealed class ServerListenOptions
    {
        public ServerListenOptions(string host, int port)
            : this(host, port, null)
        {
        }

        public ServerListenOptions(string host, int port, X509Certificate2 serverCertificate)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host is required.", nameof(host));
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

            Host = host;
            Port = port;
            ServerCertificate = serverCertificate;
        }

        public string Host { get; }

        public int Port { get; }

        public X509Certificate2 ServerCertificate { get; }
    }
}
