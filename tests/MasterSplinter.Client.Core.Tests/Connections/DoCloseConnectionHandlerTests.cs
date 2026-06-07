using MasterSplinter.Client.Core.Connections;
using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Connections
{
    [TestClass]
    public class DoCloseConnectionHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsRefreshedConnectionsAfterClose()
        {
            var provider = new TestConnectionCloseProvider(new TcpConnectionCloseResult(
                true,
                new[]
                {
                    new TcpConnection
                    {
                        ProcessName = "test",
                        LocalAddress = "127.0.0.1",
                        LocalPort = 5000,
                        RemoteAddress = "127.0.0.1",
                        RemotePort = 5001
                    }
                }));
            var handler = new DoCloseConnectionHandler(provider);

            var response = (GetConnectionsResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoCloseConnection
                {
                    LocalAddress = "127.0.0.1",
                    LocalPort = 5000,
                    RemoteAddress = "127.0.0.1",
                    RemotePort = 5001
                },
                CancellationToken.None);

            Assert.AreEqual("127.0.0.1", provider.LocalAddress);
            Assert.AreEqual((ushort)5000, provider.LocalPort);
            Assert.AreEqual("127.0.0.1", provider.RemoteAddress);
            Assert.AreEqual((ushort)5001, provider.RemotePort);
            Assert.AreEqual(1, response.Connections.Length);
        }

        private sealed class TestConnectionCloseProvider : IConnectionCloseProvider
        {
            private readonly TcpConnectionCloseResult _result;

            public TestConnectionCloseProvider(TcpConnectionCloseResult result)
            {
                _result = result;
            }

            public string LocalAddress { get; private set; }

            public ushort LocalPort { get; private set; }

            public string RemoteAddress { get; private set; }

            public ushort RemotePort { get; private set; }

            public TcpConnectionCloseResult CloseConnection(string localAddress, ushort localPort, string remoteAddress, ushort remotePort)
            {
                LocalAddress = localAddress;
                LocalPort = localPort;
                RemoteAddress = remoteAddress;
                RemotePort = remotePort;
                return _result;
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
    }
}
