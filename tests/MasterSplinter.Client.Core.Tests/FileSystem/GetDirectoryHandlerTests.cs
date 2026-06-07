using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.FileSystem
{
    [TestClass]
    public class GetDirectoryHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsDirectoryResponseFromProvider()
        {
            var items = new[]
            {
                new FileSystemEntry
                {
                    EntryType = FileType.Directory,
                    Name = "logs",
                    Size = 0,
                    LastAccessTimeUtc = new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc)
                },
                new FileSystemEntry
                {
                    EntryType = FileType.File,
                    Name = "readme.txt",
                    Size = 42,
                    ContentType = ContentType.Text,
                    LastAccessTimeUtc = new DateTime(2026, 6, 1, 12, 1, 0, DateTimeKind.Utc)
                }
            };
            var handler = new GetDirectoryHandler(new TestDirectoryProvider(
                DirectoryListResult.Success("C:\\Temp", items)));

            var response = (GetDirectoryResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetDirectory { RemotePath = "C:\\Temp" },
                CancellationToken.None);

            Assert.AreEqual("C:\\Temp", response.RemotePath);
            Assert.AreEqual(2, response.Items.Length);
            Assert.AreEqual(FileType.Directory, response.Items[0].EntryType);
            Assert.AreEqual("logs", response.Items[0].Name);
            Assert.AreEqual(FileType.File, response.Items[1].EntryType);
            Assert.AreEqual(ContentType.Text, response.Items[1].ContentType);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStatusMessageForProviderError()
        {
            var handler = new GetDirectoryHandler(new TestDirectoryProvider(
                DirectoryListResult.Error("GetDirectory Directory not found")));

            var response = (SetStatusFileManager)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetDirectory { RemotePath = "C:\\Missing" },
                CancellationToken.None);

            Assert.AreEqual("GetDirectory Directory not found", response.Message);
            Assert.IsTrue(response.SetLastDirectorySeen);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterSendsDirectoryResponseThroughCommandContext()
        {
            var adapter = new ResponseMessageHandlerAdapter<GetDirectory>(
                new GetDirectoryHandler(new TestDirectoryProvider(
                    DirectoryListResult.Success(
                        "C:\\Temp",
                        new[]
                        {
                            new FileSystemEntry
                            {
                                EntryType = FileType.File,
                                Name = "archive.zip",
                                Size = 100,
                                ContentType = ContentType.Archive
                            }
                        }))));
            var context = new RecordingCommandContext("client-1");

            await adapter.HandleAsync(
                context,
                new GetDirectory { RemotePath = "C:\\Temp" },
                CancellationToken.None);

            Assert.IsInstanceOfType(context.SentMessage, typeof(GetDirectoryResponse));
        }

        private sealed class TestDirectoryProvider : IDirectoryProvider
        {
            private readonly DirectoryListResult _result;

            public TestDirectoryProvider(DirectoryListResult result)
            {
                _result = result;
            }

            public DirectoryListResult GetDirectory(string remotePath)
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
