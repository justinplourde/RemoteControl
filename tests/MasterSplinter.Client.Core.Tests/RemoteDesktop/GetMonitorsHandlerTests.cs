using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.RemoteDesktop;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.RemoteDesktop
{
    [TestClass]
    public class GetMonitorsHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsMonitorCount()
        {
            var handler = new GetMonitorsHandler(new TestMonitorProvider(2));

            var response = (GetMonitorsResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetMonitors(),
                CancellationToken.None);

            Assert.AreEqual(2, response.Number);
        }

        private sealed class TestMonitorProvider : IMonitorProvider
        {
            private readonly int _count;

            public TestMonitorProvider(int count)
            {
                _count = count;
            }

            public int GetMonitorCount()
            {
                return _count;
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
