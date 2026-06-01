using MasterSplinter.Client.Core.Dispatch;
using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System;
using System.Net;
using System.Net.Sockets;
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
            if (dispatcher == null)
                throw new ArgumentNullException(nameof(dispatcher));

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
            private readonly NetworkStream _stream;

            public LoopbackClientCommandContext(string clientId, NetworkStream stream)
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
