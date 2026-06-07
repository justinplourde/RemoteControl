using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Networking;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Host
{
    public sealed class LoopbackTcpRemoteClientListener : IRemoteClientListener
    {
        private readonly ConcurrentDictionary<string, TcpRemoteClientConnection> _connections =
            new ConcurrentDictionary<string, TcpRemoteClientConnection>();

        private CancellationTokenSource _listenerTokenSource;
        private TcpListener _listener;
        private IRemoteClientListenerHandler _handler;

        public bool IsListening { get; private set; }

        public ServerListenOptions Options { get; private set; }

        public Task StartAsync(
            ServerListenOptions options,
            IRemoteClientListenerHandler handler,
            CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            IPAddress address = ResolveLoopbackAddress(options.Host);
            Options = options;
            _handler = handler;
            _listenerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener = new TcpListener(address, options.Port);
            _listener.Start();
            IsListening = true;
            _ = AcceptLoopAsync(_listenerTokenSource.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            IsListening = false;
            _listenerTokenSource?.Cancel();
            _listener?.Stop();

            foreach (TcpRemoteClientConnection connection in _connections.Values)
            {
                _ = connection.DisconnectAsync("Server stopped.", cancellationToken);
            }

            _connections.Clear();
            return Task.CompletedTask;
        }

        private static IPAddress ResolveLoopbackAddress(string host)
        {
            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
                return IPAddress.Loopback;

            if (!IPAddress.TryParse(host, out IPAddress address) || !IPAddress.IsLoopback(address))
                throw new InvalidOperationException("The modern TCP host is currently limited to loopback addresses.");

            return address;
        }

        private async Task AcceptLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                    Stream stream = await CreateAuthenticatedStreamAsync(client, cancellationToken).ConfigureAwait(false);
                    var connection = new TcpRemoteClientConnection(Guid.NewGuid().ToString("N"), client, stream);
                    _connections[connection.ConnectionId] = connection;
                    await _handler.ClientConnectedAsync(connection, cancellationToken).ConfigureAwait(false);
                    _ = ReadLoopAsync(connection, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Accept failed: {exception.Message}");
                }
            }
        }

        private async Task<Stream> CreateAuthenticatedStreamAsync(TcpClient client, CancellationToken cancellationToken)
        {
            NetworkStream networkStream = client.GetStream();
            if (Options.ServerCertificate == null)
                return networkStream;

            var sslStream = new SslStream(networkStream, false);
            try
            {
                await sslStream.AuthenticateAsServerAsync(
                    Options.ServerCertificate,
                    false,
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

        private async Task ReadLoopAsync(TcpRemoteClientConnection connection, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested && connection.IsConnected)
                {
                    IMessage message = connection.ReadMessage();
                    await _handler.MessageReceivedAsync(connection, message, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OverflowException) when (!cancellationToken.IsCancellationRequested)
            {
                await _handler.ClientDisconnectedAsync(connection, "Client disconnected.", cancellationToken).ConfigureAwait(false);
            }
            catch (ObjectDisposedException) when (!cancellationToken.IsCancellationRequested)
            {
                await _handler.ClientDisconnectedAsync(connection, "Client disconnected.", cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                await _handler.ClientFaultedAsync(connection, exception, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                _connections.TryRemove(connection.ConnectionId, out _);
            }
        }

        private sealed class TcpRemoteClientConnection : IRemoteClientConnection
        {
            private readonly object _sendLock = new object();
            private readonly TcpClient _client;
            private readonly Stream _stream;

            public TcpRemoteClientConnection(string connectionId, TcpClient client, Stream stream)
            {
                ConnectionId = connectionId;
                _client = client;
                _stream = stream;
                ConnectedAtUtc = DateTimeOffset.UtcNow;
                LastSeenUtc = ConnectedAtUtc;
            }

            public string ConnectionId { get; }

            public string ClientId => Identification == null ? null : Identification.Id;

            public ClientIdentification Identification { get; private set; }

            public bool IsIdentified => Identification != null;

            public bool IsConnected => _client.Connected;

            public DateTimeOffset ConnectedAtUtc { get; }

            public DateTimeOffset LastSeenUtc { get; private set; }

            public void SetIdentification(ClientIdentification identification)
            {
                Identification = identification;
            }

            public IMessage ReadMessage()
            {
                using (var reader = new PayloadReader(_stream, true))
                {
                    LastSeenUtc = DateTimeOffset.UtcNow;
                    return reader.ReadMessage();
                }
            }

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

            public Task DisconnectAsync(string reason, CancellationToken cancellationToken)
            {
                _stream.Dispose();
                _client.Close();
                return Task.CompletedTask;
            }
        }
    }
}
