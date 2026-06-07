using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.FileSystem
{
    [TestClass]
    public class DoPathDeleteHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStatusFromProvider()
        {
            var handler = new DoPathDeleteHandler(new TestPathDeleteProvider(PathDeleteResult.Success("Deleted file")));

            var response = (SetStatusFileManager)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoPathDelete { Path = "C:\\Temp\\old.txt", PathType = FileType.File },
                CancellationToken.None);

            Assert.AreEqual("Deleted file", response.Message);
            Assert.IsFalse(response.SetLastDirectorySeen);
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderDeletesFile()
        {
            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllText(path, "delete parity", Encoding.UTF8);

            PathDeleteResult result = new PathDeleteProvider().Delete(path, FileType.File);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Deleted file", result.Message);
            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderDeletesDirectoryRecursively()
        {
            string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            string childDirectory = Path.Combine(directory, "child");
            Directory.CreateDirectory(childDirectory);
            File.WriteAllText(Path.Combine(childDirectory, "nested.txt"), "delete directory parity", Encoding.UTF8);

            PathDeleteResult result = new PathDeleteProvider().Delete(directory, FileType.Directory);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Deleted directory", result.Message);
            Assert.IsFalse(Directory.Exists(directory));
        }

        private sealed class TestPathDeleteProvider : IPathDeleteProvider
        {
            private readonly PathDeleteResult _result;

            public TestPathDeleteProvider(PathDeleteResult result)
            {
                _result = result;
            }

            public PathDeleteResult Delete(string path, FileType pathType)
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
