using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.FileSystem;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.FileSystem
{
    [TestClass]
    public class DoPathRenameHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsStatusFromProvider()
        {
            var handler = new DoPathRenameHandler(new TestPathRenameProvider(
                PathRenameResult.Success("Renamed file")));

            var response = (SetStatusFileManager)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoPathRename
                {
                    Path = "C:\\Temp\\old.txt",
                    NewPath = "C:\\Temp\\new.txt",
                    PathType = FileType.File
                },
                CancellationToken.None);

            Assert.AreEqual("Renamed file", response.Message);
            Assert.IsFalse(response.SetLastDirectorySeen);
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderRenamesFileWithoutOverwriting()
        {
            string directory = CreateTempDirectory();
            string oldPath = Path.Combine(directory, "old.txt");
            string newPath = Path.Combine(directory, "new.txt");
            File.WriteAllText(oldPath, "rename parity", Encoding.UTF8);
            var provider = new PathRenameProvider();

            PathRenameResult result = provider.Rename(oldPath, newPath, FileType.File);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual("Renamed file", result.Message);
            Assert.IsFalse(File.Exists(oldPath));
            Assert.AreEqual("rename parity", File.ReadAllText(newPath, Encoding.UTF8));
        }

        [TestMethod, TestCategory("ClientCore")]
        public void ProviderRejectsExistingTarget()
        {
            string directory = CreateTempDirectory();
            string oldPath = Path.Combine(directory, "old.txt");
            string newPath = Path.Combine(directory, "new.txt");
            File.WriteAllText(oldPath, "old", Encoding.UTF8);
            File.WriteAllText(newPath, "new", Encoding.UTF8);
            var provider = new PathRenameProvider();

            PathRenameResult result = provider.Rename(oldPath, newPath, FileType.File);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual("RenamePath Target already exists", result.Message);
            Assert.IsTrue(File.Exists(oldPath));
            Assert.AreEqual("new", File.ReadAllText(newPath, Encoding.UTF8));
        }

        private static string CreateTempDirectory()
        {
            string directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
            return directory;
        }

        private sealed class TestPathRenameProvider : IPathRenameProvider
        {
            private readonly PathRenameResult _result;

            public TestPathRenameProvider(PathRenameResult result)
            {
                _result = result;
            }

            public PathRenameResult Rename(string path, string newPath, FileType pathType)
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
