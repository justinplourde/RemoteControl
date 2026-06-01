using MasterSplinter.Client.Core.Connections;
using MasterSplinter.Client.Core.Dispatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Connections
{
    [TestClass]
    public class GetConnectionsHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsConnectionsResponseFromProvider()
        {
            var handler = new GetConnectionsHandler(new TestConnectionProvider(new[]
            {
                new TcpConnection
                {
                    ProcessName = "browser",
                    LocalAddress = "127.0.0.1",
                    LocalPort = 5000,
                    RemoteAddress = "127.0.0.1",
                    RemotePort = 5001,
                    State = ConnectionState.Established
                }
            }));

            var response = (GetConnectionsResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetConnections(),
                CancellationToken.None);

            Assert.AreEqual(1, response.Connections.Length);
            Assert.AreEqual("browser", response.Connections[0].ProcessName);
            Assert.AreEqual("127.0.0.1", response.Connections[0].LocalAddress);
            Assert.AreEqual((ushort)5000, response.Connections[0].LocalPort);
            Assert.AreEqual(ConnectionState.Established, response.Connections[0].State);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerPreservesEmptyConnectionList()
        {
            var handler = new GetConnectionsHandler(new TestConnectionProvider(new TcpConnection[0]));

            var response = (GetConnectionsResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetConnections(),
                CancellationToken.None);

            Assert.AreEqual(0, response.Connections.Length);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterSendsConnectionsResponseThroughCommandContext()
        {
            var adapter = new ResponseMessageHandlerAdapter<GetConnections>(
                new GetConnectionsHandler(new TestConnectionProvider(new[]
                {
                    new TcpConnection { ProcessName = "server", LocalAddress = "0.0.0.0", LocalPort = 80 }
                })));
            var context = new RecordingCommandContext("client-1");

            await adapter.HandleAsync(context, new GetConnections(), CancellationToken.None);

            Assert.IsInstanceOfType(context.SentMessage, typeof(GetConnectionsResponse));
        }

        private sealed class TestConnectionProvider : IConnectionProvider
        {
            private readonly TcpConnection[] _connections;

            public TestConnectionProvider(TcpConnection[] connections)
            {
                _connections = connections;
            }

            public TcpConnection[] GetConnections()
            {
                return _connections;
            }
        }

        private sealed class TestClientContext : IClientContext
        {
            public TestClientContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }
        }

        private sealed class RecordingCommandContext : IClientCommandContext
        {
            public RecordingCommandContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public IMessage SentMessage { get; private set; }

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                SentMessage = message;
                return Task.CompletedTask;
            }
        }
    }
}
