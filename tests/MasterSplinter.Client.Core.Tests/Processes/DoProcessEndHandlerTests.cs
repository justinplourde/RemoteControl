using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Processes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Processes
{
    [TestClass]
    public class DoProcessEndHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsSuccessfulEndResponse()
        {
            var handler = new DoProcessEndHandler(new TestProcessEndProvider(ProcessEndResult.Success()));

            var response = (DoProcessResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoProcessEnd { Pid = 1234 },
                CancellationToken.None);

            Assert.AreEqual(ProcessAction.End, response.Action);
            Assert.IsTrue(response.Result);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsFailedEndResponse()
        {
            var handler = new DoProcessEndHandler(new TestProcessEndProvider(ProcessEndResult.Error()));

            var response = (DoProcessResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoProcessEnd { Pid = 4 },
                CancellationToken.None);

            Assert.AreEqual(ProcessAction.End, response.Action);
            Assert.IsFalse(response.Result);
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderRejectsSystemPid()
        {
            ProcessEndResult result = new ProcessEndProvider().EndProcess(4);

            Assert.IsFalse(result.IsSuccess);
        }

        private sealed class TestProcessEndProvider : IProcessEndProvider
        {
            private readonly ProcessEndResult _result;

            public TestProcessEndProvider(ProcessEndResult result)
            {
                _result = result;
            }

            public ProcessEndResult EndProcess(int pid)
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
