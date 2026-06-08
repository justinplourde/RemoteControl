using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Messages.ReverseProxy;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.ReverseProxy
{
    public sealed class ReverseProxyProvider : IReverseProxyProvider, IDisposable
    {
        private const int BufferSize = 8192;

        private readonly ConcurrentDictionary<int, TcpClient> _connections =
            new ConcurrentDictionary<int, TcpClient>();
        private readonly IReverseProxyTargetPolicy _targetPolicy;

        public ReverseProxyProvider(IReverseProxyTargetPolicy targetPolicy)
        {
            _targetPolicy = targetPolicy ?? throw new ArgumentNullException(nameof(targetPolicy));
        }

        public async Task ConnectAsync(
            ReverseProxyConnect message,
            Func<IMessage, CancellationToken, Task> sendAsync,
            CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (sendAsync == null)
                throw new ArgumentNullException(nameof(sendAsync));

            if (!_targetPolicy.IsAllowed(message.Target, message.Port))
            {
                await sendAsync(CreateConnectResponse(message, false, null, 0), cancellationToken)
                    .ConfigureAwait(false);
                return;
            }

            var client = new TcpClient();
            try
            {
                await client.ConnectAsync(message.Target, message.Port, cancellationToken).ConfigureAwait(false);
                if (!_connections.TryAdd(message.ConnectionId, client))
                {
                    client.Dispose();
                    await sendAsync(CreateConnectResponse(message, false, null, 0), cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                IPEndPoint localEndPoint = client.Client.LocalEndPoint as IPEndPoint;
                await sendAsync(CreateConnectResponse(
                    message,
                    true,
                    localEndPoint == null ? null : localEndPoint.Address.GetAddressBytes(),
                    localEndPoint == null ? 0 : localEndPoint.Port), cancellationToken)
                    .ConfigureAwait(false);

                _ = ReceiveLoopAsync(message.ConnectionId, client, sendAsync);
            }
            catch
            {
                client.Dispose();
                await sendAsync(CreateConnectResponse(message, false, null, 0), cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task SendDataAsync(
            ReverseProxyData message,
            Func<IMessage, CancellationToken, Task> sendAsync,
            CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (sendAsync == null)
                throw new ArgumentNullException(nameof(sendAsync));

            if (!_connections.TryGetValue(message.ConnectionId, out TcpClient client))
                return;

            try
            {
                byte[] data = message.Data ?? Array.Empty<byte>();
                if (data.Length > 0)
                    await client.GetStream().WriteAsync(data, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                await DisconnectAsync(new ReverseProxyDisconnect { ConnectionId = message.ConnectionId }, sendAsync, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public async Task DisconnectAsync(
            ReverseProxyDisconnect message,
            Func<IMessage, CancellationToken, Task> sendAsync,
            CancellationToken cancellationToken)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));
            if (sendAsync == null)
                throw new ArgumentNullException(nameof(sendAsync));

            if (_connections.TryRemove(message.ConnectionId, out TcpClient client))
                client.Dispose();

            await sendAsync(new ReverseProxyDisconnect { ConnectionId = message.ConnectionId }, cancellationToken)
                .ConfigureAwait(false);
        }

        public void Dispose()
        {
            foreach (TcpClient client in _connections.Values)
                client.Dispose();

            _connections.Clear();
        }

        private async Task ReceiveLoopAsync(
            int connectionId,
            TcpClient client,
            Func<IMessage, CancellationToken, Task> sendAsync)
        {
            var buffer = new byte[BufferSize];
            try
            {
                while (true)
                {
                    int received = await client.GetStream().ReadAsync(buffer, CancellationToken.None)
                        .ConfigureAwait(false);
                    if (received <= 0)
                        break;

                    var payload = new byte[received];
                    Buffer.BlockCopy(buffer, 0, payload, 0, received);
                    await sendAsync(new ReverseProxyData
                    {
                        ConnectionId = connectionId,
                        Data = payload
                    }, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch
            {
            }
            finally
            {
                if (_connections.TryRemove(connectionId, out TcpClient removed))
                    removed.Dispose();

                await sendAsync(new ReverseProxyDisconnect { ConnectionId = connectionId }, CancellationToken.None)
                    .ConfigureAwait(false);
            }
        }

        private static ReverseProxyConnectResponse CreateConnectResponse(
            ReverseProxyConnect message,
            bool isConnected,
            byte[] localAddress,
            int localPort)
        {
            return new ReverseProxyConnectResponse
            {
                ConnectionId = message.ConnectionId,
                IsConnected = isConnected,
                LocalAddress = localAddress,
                LocalPort = localPort,
                HostName = message.Target
            };
        }
    }
}
