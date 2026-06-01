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
            Assert.AreEqual("127.0.0.1", options.Host);
            Assert.AreEqual(4782, options.Port);
            Assert.AreEqual(60, options.TimeoutSeconds);
            Assert.AreEqual("cli-operator", options.OperatorId);
            Assert.IsFalse(options.GrantPermission);
            Assert.IsFalse(options.GrantConsent);
            Assert.IsFalse(options.ShowHelp);
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
                "--grant-permission",
                "--grant-consent"
            });

            Assert.AreEqual("localhost", options.Host);
            Assert.AreEqual(47831, options.Port);
            Assert.AreEqual(3, options.TimeoutSeconds);
            Assert.AreEqual("alice", options.OperatorId);
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
        }
    }
}
