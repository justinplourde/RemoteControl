using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.FileSystem
{
    [TestClass]
    public class GetDrivesHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsDrivesResponseFromProvider()
        {
            var drives = new[]
            {
                new Drive { DisplayName = "C:\\ [Local Disk, NTFS]", RootDirectory = "C:\\" }
            };
            var handler = new GetDrivesHandler(new TestDriveProvider(DriveListResult.Success(drives)));

            var response = (GetDrivesResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetDrives(),
                CancellationToken.None);

            Assert.AreEqual(1, response.Drives.Length);
            Assert.AreEqual("C:\\ [Local Disk, NTFS]", response.Drives[0].DisplayName);
            Assert.AreEqual("C:\\", response.Drives[0].RootDirectory);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStatusMessageForProviderError()
        {
            var handler = new GetDrivesHandler(new TestDriveProvider(
                DriveListResult.Error("GetDrives No permission")));

            var response = (SetStatusFileManager)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetDrives(),
                CancellationToken.None);

            Assert.AreEqual("GetDrives No permission", response.Message);
            Assert.IsFalse(response.SetLastDirectorySeen);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterSendsDrivesResponseThroughCommandContext()
        {
            var adapter = new ResponseMessageHandlerAdapter<GetDrives>(
                new GetDrivesHandler(new TestDriveProvider(
                    DriveListResult.Success(new[]
                    {
                        new Drive { DisplayName = "D:\\ [Removable Drive, FAT32]", RootDirectory = "D:\\" }
                    }))));
            var context = new RecordingCommandContext("client-1");

            await adapter.HandleAsync(context, new GetDrives(), CancellationToken.None);

            Assert.IsInstanceOfType(context.SentMessage, typeof(GetDrivesResponse));
        }

        private sealed class TestDriveProvider : IDriveProvider
        {
            private readonly DriveListResult _result;

            public TestDriveProvider(DriveListResult result)
            {
                _result = result;
            }

            public DriveListResult GetDrives()
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
