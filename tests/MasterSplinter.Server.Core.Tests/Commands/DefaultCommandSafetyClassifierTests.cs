using MasterSplinter.Server.Core.Commands;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using System.Collections.Generic;

namespace MasterSplinter.Server.Core.Tests.Commands
{
    [TestClass]
    public class DefaultCommandSafetyClassifierTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public void CompletedReadOnlyCommandsAreClassifiedAsReadOnly()
        {
            var classifier = DefaultCommandSafetyClassifier.Instance;
            var commands = new Dictionary<IMessage, CommandSafetyClass>
            {
                [new GetSystemInfo()] = CommandSafetyClass.ReadOnlyInventory,
                [new GetDrives()] = CommandSafetyClass.FileRead,
                [new GetDirectory { RemotePath = "C:\\" }] = CommandSafetyClass.FileRead,
                [new GetProcesses()] = CommandSafetyClass.ReadOnlyInventory,
                [new GetStartupItems()] = CommandSafetyClass.ReadOnlyInventory,
                [new GetConnections()] = CommandSafetyClass.ReadOnlyInventory,
                [new DoLoadRegistryKey { RootKeyName = "HKCU\\Software" }] = CommandSafetyClass.ReadOnlyInventory
            };

            foreach (KeyValuePair<IMessage, CommandSafetyClass> command in commands)
            {
                CommandSafetyMetadata metadata = classifier.Classify(command.Key);

                Assert.AreEqual(command.Value, metadata.SafetyClass, command.Key.GetType().Name);
                Assert.IsTrue(metadata.IsReadOnly, command.Key.GetType().Name);
                Assert.IsFalse(metadata.RequiresPermission, command.Key.GetType().Name);
                Assert.IsFalse(metadata.RequiresConsent, command.Key.GetType().Name);
            }
        }

        [TestMethod, TestCategory("ServerCore")]
        public void SensitiveParityTargetCommandsRequirePermissionAndConsent()
        {
            var classifier = DefaultCommandSafetyClassifier.Instance;
            var commands = new Dictionary<IMessage, CommandSafetyClass>
            {
                [new DoShellExecute { Command = "whoami" }] = CommandSafetyClass.Execution,
                [new DoProcessStart { FilePath = "C:\\Tools\\agent.exe" }] = CommandSafetyClass.Execution,
                [new DoCloseConnection { LocalAddress = "127.0.0.1", LocalPort = 5000, RemoteAddress = "127.0.0.1", RemotePort = 5001 }] = CommandSafetyClass.NetworkControl,
                [new DoPathDelete { Path = "C:\\Temp\\old.txt", PathType = FileType.File }] = CommandSafetyClass.FileWrite,
                [new DoShutdownAction { Action = ShutdownAction.Restart }] = CommandSafetyClass.SystemControl,
                [new GetPasswords()] = CommandSafetyClass.CredentialAccess,
                [new GetKeyloggerLogsDirectory()] = CommandSafetyClass.KeystrokeAccess
            };

            foreach (KeyValuePair<IMessage, CommandSafetyClass> command in commands)
            {
                CommandSafetyMetadata metadata = classifier.Classify(command.Key);

                Assert.AreEqual(command.Value, metadata.SafetyClass, command.Key.GetType().Name);
                Assert.IsFalse(metadata.IsReadOnly, command.Key.GetType().Name);
                Assert.IsTrue(metadata.RequiresPermission, command.Key.GetType().Name);
            }

            Assert.IsTrue(classifier.Classify(new DoShellExecute { Command = "whoami" }).RequiresConsent);
            Assert.IsTrue(classifier.Classify(new DoProcessStart { FilePath = "C:\\Tools\\agent.exe" }).RequiresConsent);
            Assert.IsTrue(classifier.Classify(new DoProcessEnd { Pid = 1234 }).RequiresConsent);
            Assert.IsFalse(classifier.Classify(new DoCloseConnection { LocalAddress = "127.0.0.1", LocalPort = 5000, RemoteAddress = "127.0.0.1", RemotePort = 5001 }).RequiresConsent);
            Assert.IsFalse(classifier.Classify(new DoPathDelete { Path = "C:\\Temp\\old.txt", PathType = FileType.File }).RequiresConsent);
            Assert.IsTrue(classifier.Classify(new DoShutdownAction { Action = ShutdownAction.Restart }).RequiresConsent);
            Assert.IsTrue(classifier.Classify(new GetPasswords()).RequiresConsent);
            Assert.IsTrue(classifier.Classify(new GetKeyloggerLogsDirectory()).RequiresConsent);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void FileTransferDownloadRequiresFileReadPermissionWithoutConsent()
        {
            CommandSafetyMetadata metadata = DefaultCommandSafetyClassifier.Instance.Classify(
                new FileTransferRequest { Id = 1, RemotePath = "C:\\Temp\\report.txt" });

            Assert.AreEqual(CommandSafetyClass.FileRead, metadata.SafetyClass);
            Assert.IsFalse(metadata.IsReadOnly);
            Assert.IsTrue(metadata.RequiresPermission);
            Assert.IsFalse(metadata.RequiresConsent);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void FileTransferUploadRequiresFileWritePermissionWithoutConsent()
        {
            CommandSafetyMetadata metadata = DefaultCommandSafetyClassifier.Instance.Classify(
                new FileTransferChunk
                {
                    Id = 1,
                    FilePath = "C:\\Temp\\remote.txt",
                    FileSize = 3,
                    Chunk = new MasterSplinter.Common.Models.FileChunk
                    {
                        Offset = 0,
                        Data = new byte[] { 1, 2, 3 }
                    }
                });

            Assert.AreEqual(CommandSafetyClass.FileWrite, metadata.SafetyClass);
            Assert.IsFalse(metadata.IsReadOnly);
            Assert.IsTrue(metadata.RequiresPermission);
            Assert.IsFalse(metadata.RequiresConsent);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void PathRenameRequiresFileWritePermissionWithoutConsent()
        {
            CommandSafetyMetadata metadata = DefaultCommandSafetyClassifier.Instance.Classify(
                new DoPathRename
                {
                    Path = "C:\\Temp\\old.txt",
                    NewPath = "C:\\Temp\\new.txt",
                    PathType = FileType.File
                });

            Assert.AreEqual(CommandSafetyClass.FileWrite, metadata.SafetyClass);
            Assert.IsFalse(metadata.IsReadOnly);
            Assert.IsTrue(metadata.RequiresPermission);
            Assert.IsFalse(metadata.RequiresConsent);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void PathDeleteRequiresFileWritePermissionWithoutConsent()
        {
            CommandSafetyMetadata metadata = DefaultCommandSafetyClassifier.Instance.Classify(
                new DoPathDelete
                {
                    Path = "C:\\Temp\\old.txt",
                    PathType = FileType.File
                });

            Assert.AreEqual(CommandSafetyClass.FileWrite, metadata.SafetyClass);
            Assert.IsFalse(metadata.IsReadOnly);
            Assert.IsTrue(metadata.RequiresPermission);
            Assert.IsFalse(metadata.RequiresConsent);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void UnknownCommandsAreConservative()
        {
            CommandSafetyMetadata metadata = DefaultCommandSafetyClassifier.Instance.Classify(new UnknownMessage());

            Assert.AreEqual(CommandSafetyClass.Unknown, metadata.SafetyClass);
            Assert.IsFalse(metadata.IsReadOnly);
            Assert.IsTrue(metadata.RequiresPermission);
            Assert.IsTrue(metadata.RequiresConsent);
        }

        private sealed class UnknownMessage : IMessage
        {
        }
    }
}
