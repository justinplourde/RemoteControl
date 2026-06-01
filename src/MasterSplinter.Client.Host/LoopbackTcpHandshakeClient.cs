using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Host
{
    internal sealed class LoopbackTcpHandshakeClient
    {
        public async Task<ClientIdentificationResult> IdentifyAsync(
            string host,
            int port,
            ClientIdentification identification,
            CancellationToken cancellationToken)
        {
            IPAddress address = ResolveLoopbackAddress(host);
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(address, port, cancellationToken).ConfigureAwait(false);
                using (NetworkStream stream = client.GetStream())
                {
                    using (var writer = new PayloadWriter(stream, true))
                    {
                        writer.WriteMessage(identification);
                    }

                    using (var reader = new PayloadReader(stream, true))
                    {
                        return (ClientIdentificationResult)reader.ReadMessage();
                    }
                }
            }
        }

        private static IPAddress ResolveLoopbackAddress(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return IPAddress.Loopback;

            if (!IPAddress.TryParse(host, out IPAddress address) || !IPAddress.IsLoopback(address))
                throw new InvalidOperationException("The modern TCP client is currently limited to loopback addresses.");

            return address;
        }
    }
}
