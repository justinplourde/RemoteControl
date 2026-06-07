using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.RemoteDesktop;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.RemoteDesktop
{
    [TestClass]
    public class RemoteInputHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task MouseHandlerReturnsSuccessStatus()
        {
            var provider = new TestRemoteInputProvider(RemoteInputResult.Success());
            var handler = new DoMouseEventHandler(provider);
            var message = new DoMouseEvent
            {
                Action = MouseAction.MoveCursor,
                X = 10,
                Y = 20,
                MonitorIndex = 1
            };

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                message,
                CancellationToken.None);

            Assert.AreSame(message, provider.MouseEvent);
            Assert.AreEqual("Mouse event sent.", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task KeyboardHandlerReturnsFailureStatus()
        {
            var provider = new TestRemoteInputProvider(RemoteInputResult.Error("Denied"));
            var handler = new DoKeyboardEventHandler(provider);
            var message = new DoKeyboardEvent
            {
                Key = 65,
                KeyDown = true
            };

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                message,
                CancellationToken.None);

            Assert.AreSame(message, provider.KeyboardEvent);
            Assert.AreEqual("Keyboard event failed: Denied", response.Message);
        }

        private sealed class TestRemoteInputProvider : IRemoteInputProvider
        {
            private readonly RemoteInputResult _result;

            public TestRemoteInputProvider(RemoteInputResult result)
            {
                _result = result;
            }

            public DoMouseEvent MouseEvent { get; private set; }

            public DoKeyboardEvent KeyboardEvent { get; private set; }

            public RemoteInputResult SendMouseEvent(DoMouseEvent mouseEvent)
            {
                MouseEvent = mouseEvent;
                return _result;
            }

            public RemoteInputResult SendKeyboardEvent(DoKeyboardEvent keyboardEvent)
            {
                KeyboardEvent = keyboardEvent;
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
