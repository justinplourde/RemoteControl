using System;

namespace LocationRemote.Server.Core.Listeners
{
    public sealed class ServerListenOptions
    {
        public ServerListenOptions(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host is required.", nameof(host));
            if (port < 1 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535.");

            Host = host;
            Port = port;
        }

        public string Host { get; }

        public int Port { get; }
    }
}
