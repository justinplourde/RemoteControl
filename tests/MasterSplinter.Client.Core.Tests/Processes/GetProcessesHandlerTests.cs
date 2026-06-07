using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Processes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Messages;
using System.Threading;
using System.Threading.Tasks;
using RemoteProcess = MasterSplinter.Common.Models.Process;

namespace MasterSplinter.Client.Core.Tests.Processes
{
    [TestClass]
    public class GetProcessesHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsProcessesResponseFromProvider()
        {
            var handler = new GetProcessesHandler(new TestProcessProvider(new[]
            {
                new RemoteProcess
                {
                    Name = "notepad.exe",
                    Id = 42,
                    MainWindowTitle = "notes.txt"
                }
            }));

            var response = (GetProcessesResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetProcesses(),
                CancellationToken.None);

            Assert.AreEqual(1, response.Processes.Length);
            Assert.AreEqual("notepad.exe", response.Processes[0].Name);
            Assert.AreEqual(42, response.Processes[0].Id);
            Assert.AreEqual("notes.txt", response.Processes[0].MainWindowTitle);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerPreservesEmptyProcessList()
        {
            var handler = new GetProcessesHandler(new TestProcessProvider(new RemoteProcess[0]));

            var response = (GetProcessesResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetProcesses(),
                CancellationToken.None);

            Assert.AreEqual(0, response.Processes.Length);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterSendsProcessesResponseThroughCommandContext()
        {
            var adapter = new ResponseMessageHandlerAdapter<GetProcesses>(
                new GetProcessesHandler(new TestProcessProvider(new[]
                {
                    new RemoteProcess { Name = "cmd.exe", Id = 7 }
                })));
            var context = new RecordingCommandContext("client-1");

            await adapter.HandleAsync(context, new GetProcesses(), CancellationToken.None);

            Assert.IsInstanceOfType(context.SentMessage, typeof(GetProcessesResponse));
        }

        private sealed class TestProcessProvider : IProcessProvider
        {
            private readonly RemoteProcess[] _processes;

            public TestProcessProvider(RemoteProcess[] processes)
            {
                _processes = processes;
            }

            public RemoteProcess[] GetProcesses()
            {
                return _processes;
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
