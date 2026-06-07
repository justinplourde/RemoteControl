using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Services;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Services
{
    [TestClass]
    public class DoAskElevateHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReportsAlreadyElevated()
        {
            var handler = new DoAskElevateHandler(new TestElevationRequestProvider(
                ElevationRequestResult.AlreadyElevated()));

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoAskElevate(),
                CancellationToken.None);

            Assert.AreEqual("Process already elevated.", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReportsRequestedElevation()
        {
            var handler = new DoAskElevateHandler(new TestElevationRequestProvider(
                ElevationRequestResult.Requested()));

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoAskElevate(),
                CancellationToken.None);

            Assert.AreEqual("Elevation requested.", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReportsRefusedElevation()
        {
            var handler = new DoAskElevateHandler(new TestElevationRequestProvider(
                ElevationRequestResult.Refused()));

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoAskElevate(),
                CancellationToken.None);

            Assert.AreEqual("User refused the elevation request.", response.Message);
        }

        private sealed class TestElevationRequestProvider : IElevationRequestProvider
        {
            private readonly ElevationRequestResult _result;

            public TestElevationRequestProvider(ElevationRequestResult result)
            {
                _result = result;
            }

            public ElevationRequestResult RequestElevation()
            {
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
