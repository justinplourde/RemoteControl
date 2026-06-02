using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Processes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Processes
{
    [TestClass]
    public class DoProcessStartHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsSuccessfulStartResponse()
        {
            var handler = new DoProcessStartHandler(new TestProcessStartProvider(ProcessStartResult.Success()));

            var response = (DoProcessResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoProcessStart { FilePath = "C:\\Tools\\agent.exe" },
                CancellationToken.None);

            Assert.AreEqual(ProcessAction.Start, response.Action);
            Assert.IsTrue(response.Result);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsFailedStartResponse()
        {
            var handler = new DoProcessStartHandler(new TestProcessStartProvider(ProcessStartResult.Error()));

            var response = (DoProcessResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoProcessStart { FilePath = "C:\\Missing\\agent.exe" },
                CancellationToken.None);

            Assert.AreEqual(ProcessAction.Start, response.Action);
            Assert.IsFalse(response.Result);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerRejectsDownloadAndUpdateStartRequests()
        {
            var handler = new DoProcessStartHandler(new TestProcessStartProvider(ProcessStartResult.Success()));

            var downloadResponse = (DoProcessResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoProcessStart { DownloadUrl = "https://example.invalid/a.exe" },
                CancellationToken.None);
            var updateResponse = (DoProcessResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoProcessStart { FilePath = "C:\\Tools\\agent.exe", IsUpdate = true },
                CancellationToken.None);

            Assert.IsFalse(downloadResponse.Result);
            Assert.IsFalse(updateResponse.Result);
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderRejectsBlankPath()
        {
            ProcessStartResult result = new ProcessStartProvider().StartProcess(null);

            Assert.IsFalse(result.IsSuccess);
        }

        private sealed class TestProcessStartProvider : IProcessStartProvider
        {
            private readonly ProcessStartResult _result;

            public TestProcessStartProvider(ProcessStartResult result)
            {
                _result = result;
            }

            public ProcessStartResult StartProcess(string filePath)
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
