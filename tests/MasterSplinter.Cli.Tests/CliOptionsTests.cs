using System;
using System.Collections.Generic;
using MasterSplinter.Cli;
using Microsoft.Win32;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;

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
            Assert.IsNull(options.NewPath);
            Assert.IsNull(options.PathType);
            Assert.IsFalse(options.Pid.HasValue);
            Assert.IsNull(options.Action);
            Assert.IsNull(options.LocalAddress);
            Assert.IsFalse(options.LocalPort.HasValue);
            Assert.IsNull(options.RemoteAddress);
            Assert.IsFalse(options.RemotePort.HasValue);
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
                "--new-path", "C:\\Temp\\new.bin",
                "--type", "file",
                "--pid", "1234",
                "--action", "restart",
                "--local-address", "127.0.0.1",
                "--local-port", "5000",
                "--remote-address", "127.0.0.1",
                "--remote-port", "5001",
                "--remote-path", "C:\\Temp\\remote.bin",
                "--output", "C:\\Temp\\out.bin",
                "--grant-permission",
                "--grant-consent"
            });

            Assert.AreEqual("localhost", options.Host);
            Assert.AreEqual(47831, options.Port);
            Assert.AreEqual(3, options.TimeoutSeconds);
            Assert.AreEqual("alice", options.OperatorId);
            Assert.AreEqual("C:\\Temp\\new.bin", options.NewPath);
            Assert.AreEqual("file", options.PathType);
            Assert.AreEqual(1234, options.Pid);
            Assert.AreEqual("restart", options.Action);
            Assert.AreEqual("127.0.0.1", options.LocalAddress);
            Assert.AreEqual((ushort)5000, options.LocalPort);
            Assert.AreEqual("127.0.0.1", options.RemoteAddress);
            Assert.AreEqual((ushort)5001, options.RemotePort);
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

            CliOptions rename = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "rename-path",
                "--path", "C:\\Temp\\old.txt",
                "--new-path", "C:\\Temp\\new.txt",
                "--type", "file"
            });

            Assert.AreEqual("rename-path", rename.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\old.txt", rename.Path);
            Assert.AreEqual("C:\\Temp\\new.txt", rename.NewPath);
            Assert.AreEqual("file", rename.PathType);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "rename-path", "--path", "C:\\Temp\\old.txt", "--type", "file" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "rename-path", "--path", "C:\\Temp\\old.txt", "--new-path", "C:\\Temp\\new.txt" }));

            CliOptions delete = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "delete-path",
                "--path", "C:\\Temp\\old.txt",
                "--type", "file"
            });

            Assert.AreEqual("delete-path", delete.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\old.txt", delete.Path);
            Assert.AreEqual("file", delete.PathType);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "delete-path", "--path", "C:\\Temp\\old.txt" }));

            CliOptions endProcess = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "end-process",
                "--pid", "4321"
            });

            Assert.AreEqual("end-process", endProcess.DispatchCommand);
            Assert.AreEqual(4321, endProcess.Pid);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "end-process" }));

            CliOptions startProcess = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "start-process",
                "--path", "C:\\Temp\\run.cmd"
            });

            Assert.AreEqual("start-process", startProcess.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\run.cmd", startProcess.Path);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "start-process" }));

            CliOptions registryKey = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "get-registry-key",
                "--path", "HKCU\\Software"
            });

            Assert.AreEqual("get-registry-key", registryKey.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryKey.Path);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "get-registry-key" }));

            CliOptions closeConnection = CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "close-connection",
                "--local-address", "127.0.0.1",
                "--local-port", "5000",
                "--remote-address", "127.0.0.1",
                "--remote-port", "5001"
            });

            Assert.AreEqual("close-connection", closeConnection.DispatchCommand);
            Assert.AreEqual("127.0.0.1", closeConnection.LocalAddress);
            Assert.AreEqual((ushort)5000, closeConnection.LocalPort);
            Assert.AreEqual("127.0.0.1", closeConnection.RemoteAddress);
            Assert.AreEqual((ushort)5001, closeConnection.RemotePort);
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "close-connection" }));
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

            var registryKey = (DoLoadRegistryKey)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "get-registry-key",
                "--path", "HKCU\\Software"
            }));
            Assert.AreEqual("HKCU\\Software", registryKey.RootKeyName);

            var closeConnection = (DoCloseConnection)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "close-connection",
                "--local-address", "127.0.0.1",
                "--local-port", "5000",
                "--remote-address", "127.0.0.1",
                "--remote-port", "5001"
            }));
            Assert.AreEqual("127.0.0.1", closeConnection.LocalAddress);
            Assert.AreEqual((ushort)5000, closeConnection.LocalPort);
            Assert.AreEqual("127.0.0.1", closeConnection.RemoteAddress);
            Assert.AreEqual((ushort)5001, closeConnection.RemotePort);

            var rename = (DoPathRename)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "rename-path",
                "--path", "C:\\Temp\\old.txt",
                "--new-path", "C:\\Temp\\new.txt",
                "--type", "file"
            }));
            Assert.AreEqual("C:\\Temp\\old.txt", rename.Path);
            Assert.AreEqual("C:\\Temp\\new.txt", rename.NewPath);
            Assert.AreEqual(FileType.File, rename.PathType);

            var delete = (DoPathDelete)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "delete-path",
                "--path", "C:\\Temp\\old.txt",
                "--type", "file"
            }));
            Assert.AreEqual("C:\\Temp\\old.txt", delete.Path);
            Assert.AreEqual(FileType.File, delete.PathType);

            var endProcess = (DoProcessEnd)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "end-process",
                "--pid", "4321"
            }));
            Assert.AreEqual(4321, endProcess.Pid);

            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "ask-elevate" })),
                typeof(DoAskElevate));

            var shutdownAction = (DoShutdownAction)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "shutdown-action",
                "--action", "restart"
            }));
            Assert.AreEqual(ShutdownAction.Restart, shutdownAction.Action);

            var startProcess = (DoProcessStart)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "start-process",
                "--path", "C:\\Temp\\run.cmd"
            }));
            Assert.AreEqual("C:\\Temp\\run.cmd", startProcess.FilePath);
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
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "shutdown-action" }));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "shutdown-action", "--action", "bogus" })));
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

            ListenCommand rename = ListenCommand.Parse("dispatch first rename-path --path C:\\Temp\\old.txt --new-path C:\\Temp\\new.txt --type file");
            Assert.AreEqual("rename-path", rename.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\old.txt", rename.Path);
            Assert.AreEqual("C:\\Temp\\new.txt", rename.NewPath);
            Assert.AreEqual("file", rename.PathType);

            ListenCommand delete = ListenCommand.Parse("dispatch first delete-path --path C:\\Temp\\old.txt --type file");
            Assert.AreEqual("delete-path", delete.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\old.txt", delete.Path);
            Assert.AreEqual("file", delete.PathType);

            ListenCommand endProcess = ListenCommand.Parse("dispatch first end-process --pid 4321");
            Assert.AreEqual("end-process", endProcess.DispatchCommand);
            Assert.AreEqual(4321, endProcess.Pid);

            ListenCommand askElevate = ListenCommand.Parse("dispatch first ask-elevate");
            Assert.AreEqual("ask-elevate", askElevate.DispatchCommand);

            ListenCommand shutdownAction = ListenCommand.Parse("dispatch first shutdown-action --action standby");
            Assert.AreEqual("shutdown-action", shutdownAction.DispatchCommand);
            Assert.AreEqual("standby", shutdownAction.Action);

            ListenCommand startProcess = ListenCommand.Parse("dispatch first start-process --path C:\\Temp\\run.cmd");
            Assert.AreEqual("start-process", startProcess.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\run.cmd", startProcess.Path);

            ListenCommand registryKey = ListenCommand.Parse("dispatch first get-registry-key --path HKCU\\Software");
            Assert.AreEqual("get-registry-key", registryKey.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryKey.Path);

            ListenCommand closeConnection = ListenCommand.Parse("dispatch first close-connection --local-address 127.0.0.1 --local-port 5000 --remote-address 127.0.0.1 --remote-port 5001");
            Assert.AreEqual("close-connection", closeConnection.DispatchCommand);
            Assert.AreEqual("127.0.0.1", closeConnection.LocalAddress);
            Assert.AreEqual((ushort)5000, closeConnection.LocalPort);
            Assert.AreEqual("127.0.0.1", closeConnection.RemoteAddress);
            Assert.AreEqual((ushort)5001, closeConnection.RemotePort);

            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first get-directory"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first download-file"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first upload-file --path C:\\Temp\\local.txt"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first rename-path --path C:\\Temp\\old.txt --type file"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first delete-path --path C:\\Temp\\old.txt"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first end-process"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first shutdown-action"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first start-process"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first get-registry-key"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first close-connection"));
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
                        new MasterSplinter.Common.Models.Process
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
                new[] { "Registry key: HKCU\\Software; Matches=1; IsError=False; Error=-.", "- Classes Values=1 HasSubKeys=True" },
                Program.FormatResponse(new GetRegistryKeysResponse
                {
                    RootKey = "HKCU\\Software",
                    Matches = new[]
                    {
                        new RegSeekerMatch
                        {
                            Key = "Classes",
                            HasSubKeys = true,
                            Data = new[]
                            {
                                new RegValueData
                                {
                                    Name = "",
                                    Kind = (RegistryValueKind)1,
                                    Data = new byte[0]
                                }
                            }
                        }
                    }
                }));

            CollectionAssert.AreEqual(
                new[] { "File manager status: Renamed file; SetLastDirectorySeen=False." },
                Program.FormatResponse(new SetStatusFileManager
                {
                    Message = "Renamed file",
                    SetLastDirectorySeen = false
                }));

            CollectionAssert.AreEqual(
                new[] { "Status: Process already elevated." },
                Program.FormatResponse(new SetStatus
                {
                    Message = "Process already elevated."
                }));

            CollectionAssert.AreEqual(
                new[] { "Process response: Action=End; Result=True." },
                Program.FormatResponse(new DoProcessResponse
                {
                    Action = ProcessAction.End,
                    Result = true
                }));

            CollectionAssert.AreEqual(
                new[] { "Process response: Action=Start; Result=True." },
                Program.FormatResponse(new DoProcessResponse
                {
                    Action = ProcessAction.Start,
                    Result = true
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
