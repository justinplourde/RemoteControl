using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
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
            return await IdentifyAsync(host, port, identification, null, cancellationToken).ConfigureAwait(false);
        }

        public async Task<ClientIdentificationResult> IdentifyAsync(
            string host,
            int port,
            ClientIdentification identification,
            X509Certificate2 expectedServerCertificate,
            CancellationToken cancellationToken)
        {
            IPAddress address = ResolveLoopbackAddress(host);
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(address, port, cancellationToken).ConfigureAwait(false);
                using (Stream stream = await CreateAuthenticatedStreamAsync(
                    client,
                    address,
                    expectedServerCertificate,
                    cancellationToken).ConfigureAwait(false))
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

        private static async Task<Stream> CreateAuthenticatedStreamAsync(
            TcpClient client,
            IPAddress address,
            X509Certificate2 expectedServerCertificate,
            CancellationToken cancellationToken)
        {
            NetworkStream networkStream = client.GetStream();
            if (expectedServerCertificate == null)
                return networkStream;

            var sslStream = new SslStream(
                networkStream,
                false,
                (sender, certificate, chain, errors) => expectedServerCertificate.Equals(certificate));
            try
            {
                await sslStream.AuthenticateAsClientAsync(
                    address.ToString(),
                    null,
                    SslProtocols.Tls12,
                    false).WaitAsync(cancellationToken).ConfigureAwait(false);
                return sslStream;
            }
            catch
            {
                sslStream.Dispose();
                throw;
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
