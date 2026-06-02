using System;
using System.Collections.Generic;
using MasterSplinter.Cli;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Models;

namespace MasterSplinter.Cli.Tests
{
    [TestClass]
    public class CliOptionsTests
    {
        [TestMethod, TestCategory("Cli")]
        public void ParseDispatchDefaults()
        {
            CliOptions options = CliOptions.Parse(new[] { "dispatch", "--command", "get-system-info" });

            Assert.AreEqual("dispatch", options.Command);
            Assert.AreEqual("get-system-info", options.DispatchCommand);
            Assert.IsNull(options.Path);
            Assert.IsNull(options.RemotePath);
            Assert.IsNull(options.OutputPath);
            Assert.AreEqual("127.0.0.1", options.Host);
            Assert.AreEqual(4782, options.Port);
            Assert.AreEqual(60, options.TimeoutSeconds);
            Assert.AreEqual("cli-operator", options.OperatorId);
            Assert.IsFalse(options.GrantPermission);
            Assert.IsFalse(options.GrantConsent);
            Assert.IsFalse(options.ShowHelp);
        }

        [TestMethod, TestCategory("Cli")]
        public void ParseListenDefaults()
        {
            CliOptions options = CliOptions.Parse(new[] { "listen" });

            Assert.AreEqual("listen", options.Command);
            Assert.IsNull(options.DispatchCommand);
            Assert.AreEqual("127.0.0.1", options.Host);
            Assert.AreEqual(4782, options.Port);
            Assert.AreEqual(60, options.TimeoutSeconds);
        }

        [TestMethod, TestCategory("Cli")]
        public void ParseDispatchCustomOptions()
        {
            CliOptions options = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "get-system-info",
                "--host", "localhost",
                "--port", "47831",
                "--timeout-seconds", "3",
                "--operator-id", "alice",
                "--remote-path", "C:\\Temp\\remote.bin",
                "--output", "C:\\Temp\\out.bin",
                "--grant-permission",
                "--grant-consent"
            });

            Assert.AreEqual("localhost", options.Host);
            Assert.AreEqual(47831, options.Port);
            Assert.AreEqual(3, options.TimeoutSeconds);
            Assert.AreEqual("alice", options.OperatorId);
            Assert.AreEqual("C:\\Temp\\remote.bin", options.RemotePath);
            Assert.AreEqual("C:\\Temp\\out.bin", options.OutputPath);
            Assert.IsTrue(options.GrantPermission);
            Assert.IsTrue(options.GrantConsent);
        }

        [TestMethod, TestCategory("Cli")]
        public void ParseDispatchDirectoryRequiresPath()
        {
            CliOptions options = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "get-directory",
                "--path", "C:\\Temp"
            });

            Assert.AreEqual("get-directory", options.DispatchCommand);
            Assert.AreEqual("C:\\Temp", options.Path);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "get-directory" }));

            CliOptions download = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "download-file",
                "--path", "C:\\Temp\\report.txt",
                "--output", "C:\\Temp\\report.copy.txt"
            });

            Assert.AreEqual("download-file", download.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\report.txt", download.Path);
            Assert.AreEqual("C:\\Temp\\report.copy.txt", download.OutputPath);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "download-file" }));

            CliOptions upload = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "upload-file",
                "--path", "C:\\Temp\\local.txt",
                "--remote-path", "C:\\Temp\\remote.txt"
            });

            Assert.AreEqual("upload-file", upload.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\local.txt", upload.Path);
            Assert.AreEqual("C:\\Temp\\remote.txt", upload.RemotePath);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "upload-file", "--path", "C:\\Temp\\local.txt" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "upload-file", "--remote-path", "C:\\Temp\\remote.txt" }));
        }

        [TestMethod, TestCategory("Cli")]
        public void CreateMessageSupportsReadOnlyDispatchCommands()
        {
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "get-system-info" })),
                typeof(GetSystemInfo));
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "get-drives" })),
                typeof(GetDrives));
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "get-processes" })),
                typeof(GetProcesses));
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "get-startup-items" })),
                typeof(GetStartupItems));
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "get-connections" })),
                typeof(GetConnections));

            var directory = (GetDirectory)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "get-directory",
                "--path", "C:\\Temp"
            }));
            Assert.AreEqual("C:\\Temp", directory.RemotePath);

            var download = (FileTransferRequest)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "download-file",
                "--path", "C:\\Temp\\report.txt"
            }));
            Assert.AreEqual(1, download.Id);
            Assert.AreEqual("C:\\Temp\\report.txt", download.RemotePath);
        }

        [TestMethod, TestCategory("Cli")]
        public void ParseHelpAndRejectsInvalidInputs()
        {
            Assert.IsTrue(CliOptions.Parse(new[] { "--help" }).ShowHelp);
            Assert.ThrowsException<ArgumentException>(() => CliOptions.Parse(new[] { "dispatch" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "get-system-info", "--nope" }));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "unknown" })));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[]
                {
                    "dispatch",
                    "--command", "upload-file",
                    "--path", "C:\\Temp\\local.txt",
                    "--remote-path", "C:\\Temp\\remote.txt"
                })));
        }

        [TestMethod, TestCategory("Cli")]
        public void ParseListenCommands()
        {
            ListenCommand clients = ListenCommand.Parse("clients");
            Assert.AreEqual("clients", clients.Verb);

            ListenCommand exit = ListenCommand.Parse("exit");
            Assert.AreEqual("exit", exit.Verb);

            ListenCommand dispatch = ListenCommand.Parse("dispatch first get-directory --path \"C:\\Program Files\"");
            Assert.AreEqual("dispatch", dispatch.Verb);
            Assert.AreEqual("first", dispatch.ClientId);
            Assert.AreEqual("get-directory", dispatch.DispatchCommand);
            Assert.AreEqual("C:\\Program Files", dispatch.Path);

            ListenCommand download = ListenCommand.Parse("dispatch first download-file --path C:\\Temp\\report.txt --output C:\\Temp\\copy.txt");
            Assert.AreEqual("download-file", download.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\report.txt", download.Path);
            Assert.AreEqual("C:\\Temp\\copy.txt", download.OutputPath);

            ListenCommand upload = ListenCommand.Parse("dispatch first upload-file --path C:\\Temp\\local.txt --remote-path C:\\Temp\\remote.txt");
            Assert.AreEqual("upload-file", upload.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\local.txt", upload.Path);
            Assert.AreEqual("C:\\Temp\\remote.txt", upload.RemotePath);

            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first get-directory"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first download-file"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first upload-file --path C:\\Temp\\local.txt"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("bogus"));
        }

        [TestMethod, TestCategory("Cli")]
        public void FormatResponsePrintsPayloadRows()
        {
            CollectionAssert.AreEqual(
                new[] { "Drives: 1.", "- C:\\ (OS) [Local Disk, NTFS] => C:\\" },
                Program.FormatResponse(new GetDrivesResponse
                {
                    Drives = new[]
                    {
                        new Drive
                        {
                            DisplayName = "C:\\ (OS) [Local Disk, NTFS]",
                            RootDirectory = "C:\\"
                        }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "Directory path: C:\\Temp; Items: 1.", "- File report.pdf Size=42" },
                Program.FormatResponse(new GetDirectoryResponse
                {
                    RemotePath = "C:\\Temp",
                    Items = new[]
                    {
                        new FileSystemEntry
                        {
                            EntryType = FileType.File,
                            Name = "report.pdf",
                            Size = 42
                        }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "Processes: 1.", "- PID=123 notepad Title=Untitled" },
                Program.FormatResponse(new GetProcessesResponse
                {
                    Processes = new[]
                    {
                        new Quasar.Common.Models.Process
                        {
                            Id = 123,
                            Name = "notepad",
                            MainWindowTitle = "Untitled"
                        }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "Startup items: 1.", "- CurrentUserRun Updater => C:\\updater.exe" },
                Program.FormatResponse(new GetStartupItemsResponse
                {
                    StartupItems = new List<StartupItem>
                    {
                        new StartupItem
                        {
                            Type = StartupType.CurrentUserRun,
                            Name = "Updater",
                            Path = "C:\\updater.exe"
                        }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "TCP connections: 1.", "- app 127.0.0.1:1234 -> 127.0.0.1:4782 Established" },
                Program.FormatResponse(new GetConnectionsResponse
                {
                    Connections = new[]
                    {
                        new TcpConnection
                        {
                            ProcessName = "app",
                            LocalAddress = "127.0.0.1",
                            LocalPort = 1234,
                            RemoteAddress = "127.0.0.1",
                            RemotePort = 4782,
                            State = ConnectionState.Established
                        }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "System info entries: 1.", "- OS: Windows" },
                Program.FormatResponse(new GetSystemInfoResponse
                {
                    SystemInfos = new List<Tuple<string, string>>
                    {
                        Tuple.Create("OS", "Windows")
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "File transfer chunk: Id=7; Offset=10; Bytes=3; FileSize=13; Path=C:\\Temp\\a.txt." },
                Program.FormatResponse(new FileTransferChunk
                {
                    Id = 7,
                    FilePath = "C:\\Temp\\a.txt",
                    FileSize = 13,
                    Chunk = new FileChunk
                    {
                        Offset = 10,
                        Data = new byte[] { 1, 2, 3 }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "File transfer complete: Id=7; Path=C:\\Temp\\a.txt." },
                Program.FormatResponse(new FileTransferComplete { Id = 7, FilePath = "C:\\Temp\\a.txt" }));

            CollectionAssert.AreEqual(
                new[] { "File transfer canceled: Id=7; Reason=No permission." },
                Program.FormatResponse(new FileTransferCancel { Id = 7, Reason = "No permission" }));
        }
    }
}
