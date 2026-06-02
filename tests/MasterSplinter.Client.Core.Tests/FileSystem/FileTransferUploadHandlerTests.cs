using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using Quasar.Common.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.FileSystem
{
    [TestClass]
    public class FileTransferUploadHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerWritesChunksAndSendsCompletion()
        {
            string directory = CreateTempDirectory();
            string target = Path.Combine(directory, "uploaded.txt");
            byte[] content = Encoding.UTF8.GetBytes("upload parity");
            var handler = new FileTransferUploadHandler(new FileUploadProvider());
            var context = new RecordingCommandContext("client-1");

            await handler.HandleAsync(context, CreateChunk(9, target, content.Length, 0, content.Take(6).ToArray()), CancellationToken.None);
            await handler.HandleAsync(context, CreateChunk(9, target, content.Length, 6, content.Skip(6).ToArray()), CancellationToken.None);

            var complete = (FileTransferComplete)context.SentMessages.Single();
            Assert.AreEqual(9, complete.Id);
            Assert.AreEqual(Path.GetFullPath(target), complete.FilePath);
            CollectionAssert.AreEqual(content, File.ReadAllBytes(target));
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerSendsCancelForOffsetMismatchAndRemovesTempFile()
        {
            string directory = CreateTempDirectory();
            string target = Path.Combine(directory, "bad.bin");
            var handler = new FileTransferUploadHandler(new FileUploadProvider());
            var context = new RecordingCommandContext("client-1");

            await handler.HandleAsync(context, CreateChunk(10, target, 4, 0, new byte[] { 1, 2 }), CancellationToken.None);
            await handler.HandleAsync(context, CreateChunk(10, target, 4, 3, new byte[] { 3, 4 }), CancellationToken.None);

            var cancel = (FileTransferCancel)context.SentMessages.Single();
            Assert.AreEqual(10, cancel.Id);
            Assert.AreEqual("FileTransferChunk Offset mismatch", cancel.Reason);
            Assert.IsFalse(File.Exists(target));
            Assert.IsFalse(File.Exists(target + ".uploading-10"));
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerAcknowledgesCancelAndRemovesTempFile()
        {
            string directory = CreateTempDirectory();
            string target = Path.Combine(directory, "cancel.bin");
            var handler = new FileTransferUploadHandler(new FileUploadProvider());
            var context = new RecordingCommandContext("client-1");

            await handler.HandleAsync(context, CreateChunk(11, target, 4, 0, new byte[] { 1, 2 }), CancellationToken.None);
            await handler.HandleAsync(context, new FileTransferCancel { Id = 11, Reason = "Operator canceled" }, CancellationToken.None);

            var cancel = (FileTransferCancel)context.SentMessages.Single();
            Assert.AreEqual("Operator canceled", cancel.Reason);
            Assert.IsFalse(File.Exists(target));
            Assert.IsFalse(File.Exists(target + ".uploading-11"));
        }

        private static FileTransferChunk CreateChunk(int id, string path, long fileSize, long offset, byte[] data)
        {
            return new FileTransferChunk
            {
                Id = id,
                FilePath = path,
                FileSize = fileSize,
                Chunk = new FileChunk
                {
                    Offset = offset,
                    Data = data
                }
            };
        }

        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            return directory;
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
