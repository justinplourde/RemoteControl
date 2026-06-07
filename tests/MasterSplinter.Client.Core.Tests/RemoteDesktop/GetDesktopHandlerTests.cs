using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.RemoteDesktop;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Video;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.RemoteDesktop
{
    [TestClass]
    public class GetDesktopHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsCapturedDesktopFrame()
        {
            var provider = new TestDesktopCaptureProvider();
            var handler = new GetDesktopHandler(provider);
            var message = new GetDesktop
            {
                CreateNew = true,
                Quality = 80,
                DisplayIndex = 1
            };

            var response = (GetDesktopResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                message,
                CancellationToken.None);

            Assert.AreSame(message, provider.Request);
            Assert.AreEqual(80, response.Quality);
            Assert.AreEqual(1, response.Monitor);
            Assert.AreEqual(640, response.Resolution.Width);
            Assert.AreEqual(480, response.Resolution.Height);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, response.Image);
        }

        private sealed class TestDesktopCaptureProvider : IDesktopCaptureProvider
        {
            public GetDesktop Request { get; private set; }

            public GetDesktopResponse Capture(GetDesktop request)
            {
                Request = request;
                return new GetDesktopResponse
                {
                    Image = new byte[] { 1, 2, 3 },
                    Quality = request.Quality,
                    Monitor = request.DisplayIndex,
                    Resolution = new Resolution { Width = 640, Height = 480 }
                };
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
