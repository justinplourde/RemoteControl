using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Shell;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Shell
{
    [TestClass]
    public class DoShellExecuteHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsShellOutputResponse()
        {
            var provider = new RecordingProvider(ShellCommandResult.Success("hello"));
            var handler = new DoShellExecuteHandler(provider);

            var response = (DoShellExecuteResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoShellExecute { Command = "whoami" },
                CancellationToken.None);

            Assert.AreEqual("whoami", provider.Command);
            Assert.AreEqual("hello", response.Output);
            Assert.IsFalse(response.IsError);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsShellErrorResponse()
        {
            var provider = new RecordingProvider(ShellCommandResult.Error("failed"));
            var handler = new DoShellExecuteHandler(provider);

            var response = (DoShellExecuteResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoShellExecute { Command = "bad" },
                CancellationToken.None);

            Assert.AreEqual("failed", response.Output);
            Assert.IsTrue(response.IsError);
        }

        private sealed class RecordingProvider : IShellCommandProvider
        {
            private readonly ShellCommandResult _result;

            public RecordingProvider(ShellCommandResult result)
            {
                _result = result;
            }

            public string Command { get; private set; }

            public Task<ShellCommandResult> ExecuteAsync(string command, CancellationToken cancellationToken)
            {
                Command = command;
                return Task.FromResult(_result);
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
