using MasterSplinter.Client.Core.Dispatch;
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
    internal sealed class LoopbackTcpCommandClient
    {
        public async Task<ClientIdentificationResult> IdentifyAndHandleOneCommandAsync(
            string host,
            int port,
            ClientIdentification identification,
            IMessageDispatcher dispatcher,
            CancellationToken cancellationToken)
        {
            return await IdentifyAndHandleOneCommandAsync(
                host,
                port,
                identification,
                dispatcher,
                null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<ClientIdentificationResult> IdentifyAndHandleCommandsAsync(
            string host,
            int port,
            ClientIdentification identification,
            IMessageDispatcher dispatcher,
            CancellationToken cancellationToken)
        {
            return await IdentifyAndHandleCommandsAsync(
                host,
                port,
                identification,
                dispatcher,
                null,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<ClientIdentificationResult> IdentifyAndHandleOneCommandAsync(
            string host,
            int port,
            ClientIdentification identification,
            IMessageDispatcher dispatcher,
            X509Certificate2 expectedServerCertificate,
            CancellationToken cancellationToken)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

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

                    ClientIdentificationResult result;
                    using (var reader = new PayloadReader(stream, true))
                    {
                        result = (ClientIdentificationResult)reader.ReadMessage();
                    }

                    if (!result.Result)
                        return result;

                    IMessage command;
                    using (var reader = new PayloadReader(stream, true))
                    {
                        command = reader.ReadMessage();
                    }

                    var context = new LoopbackClientCommandContext(identification.Id, stream);
                    await dispatcher.DispatchAsync(context, command, cancellationToken).ConfigureAwait(false);
                    return result;
                }
            }
        }

        public async Task<ClientIdentificationResult> IdentifyAndHandleCommandsAsync(
            string host,
            int port,
            ClientIdentification identification,
            IMessageDispatcher dispatcher,
            X509Certificate2 expectedServerCertificate,
            CancellationToken cancellationToken)
        {
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

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

                    ClientIdentificationResult result;
                    using (var reader = new PayloadReader(stream, true))
                    {
                        result = (ClientIdentificationResult)reader.ReadMessage();
                    }

                    if (!result.Result)
                        return result;

                    var context = new LoopbackClientCommandContext(identification.Id, stream);
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        IMessage command;
                        try
                        {
                            using (var reader = new PayloadReader(stream, true))
                            {
                                command = reader.ReadMessage();
                            }
                        }
                        catch (IOException)
                        {
                            return result;
                        }
                        catch (OverflowException)
                        {
                            return result;
                        }

                        await dispatcher.DispatchAsync(context, command, cancellationToken).ConfigureAwait(false);
                    }

                    return result;
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

        private sealed class LoopbackClientCommandContext : IClientCommandContext
        {
            private readonly object _sendLock = new object();
            private readonly Stream _stream;

            public LoopbackClientCommandContext(string clientId, Stream stream)
            {
                ClientId = clientId;
                _stream = stream;
            }

            public string ClientId { get; }

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                lock (_sendLock)
                {
                    using (var writer = new PayloadWriter(_stream, true))
                    {
                        writer.WriteMessage(message);
                    }
                }

                return Task.CompletedTask;
            }
        }
    }
}
