using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.FileSystem
{
    [TestClass]
    public class FileTransferRequestHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerStreamsFileChunksAndCompletion()
        {
            byte[] content = Encoding.UTF8.GetBytes("hello-file-transfer");
            var handler = new FileTransferRequestHandler(
                new TestFileDownloadProvider(FileDownloadResult.Success(
                    "C:\\Temp\\hello.txt",
                    content.Length,
                    new MemoryStream(content))),
                chunkSize: 5);
            var context = new RecordingCommandContext("client-1");

            await handler.HandleAsync(
                context,
                new FileTransferRequest { Id = 7, RemotePath = "C:\\Temp\\hello.txt" },
                CancellationToken.None);

            List<FileTransferChunk> chunks = context.SentMessages.OfType<FileTransferChunk>().ToList();
            Assert.AreEqual(4, chunks.Count);
            Assert.AreEqual(0, chunks[0].Chunk.Offset);
            Assert.AreEqual(5, chunks[1].Chunk.Offset);
            Assert.AreEqual(content.Length, chunks[0].FileSize);
            CollectionAssert.AreEqual(content, chunks.SelectMany(chunk => chunk.Chunk.Data).ToArray());

            var complete = (FileTransferComplete)context.SentMessages.Last();
            Assert.AreEqual(7, complete.Id);
            Assert.AreEqual("C:\\Temp\\hello.txt", complete.FilePath);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerSendsCancelForProviderError()
        {
            var handler = new FileTransferRequestHandler(
                new TestFileDownloadProvider(FileDownloadResult.Error("FileTransferRequest File not found")));
            var context = new RecordingCommandContext("client-1");

            await handler.HandleAsync(
                context,
                new FileTransferRequest { Id = 8, RemotePath = "C:\\Missing.txt" },
                CancellationToken.None);

            var cancel = (FileTransferCancel)context.SentMessages.Single();
            Assert.AreEqual(8, cancel.Id);
            Assert.AreEqual("FileTransferRequest File not found", cancel.Reason);
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderRejectsOversizedFiles()
        {
            string path = Path.GetTempFileName();
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });
                var provider = new FileDownloadProvider(maxFileSizeBytes: 3);

                FileDownloadResult result = provider.OpenRead(path);

                Assert.IsFalse(result.IsSuccess);
                Assert.AreEqual("FileTransferRequest File exceeds size limit", result.ErrorMessage);
            }
            finally
            {
                File.Delete(path);
            }
        }

        private sealed class TestFileDownloadProvider : IFileDownloadProvider
        {
            private readonly FileDownloadResult _result;

            public TestFileDownloadProvider(FileDownloadResult result)
            {
                _result = result;
            }

            public FileDownloadResult OpenRead(string remotePath)
            {
                return _result;
            }
        }

        private sealed class RecordingCommandContext : IClientCommandContext
        {
            public RecordingCommandContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public List<IMessage> SentMessages { get; } = new List<IMessage>();

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                SentMessages.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
