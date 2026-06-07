using System;
using System.Collections.Generic;
using MasterSplinter.Cli;
using Microsoft.Win32;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Models;

namespace MasterSplinter.Cli.Tests
{
#pragma warning disable CA1416
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
            Assert.IsNull(options.Name);
            Assert.IsNull(options.NewName);
            Assert.IsNull(options.StartupType);
            Assert.IsFalse(options.Pid.HasValue);
            Assert.IsNull(options.Action);
            Assert.IsNull(options.Caption);
            Assert.IsNull(options.Text);
            Assert.IsNull(options.Button);
            Assert.IsNull(options.Icon);
            Assert.IsNull(options.Url);
            Assert.IsFalse(options.Hidden);
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
                "--name", "Agent",
                "--new-name", "Renamed",
                "--startup-type", "current-user-run",
                "--pid", "1234",
                "--action", "restart",
                "--caption", "Notice",
                "--text", "Hello",
                "--button", "OKCancel",
                "--icon", "Information",
                "--url", "https://example.test",
                "--quality", "80",
                "--display-index", "2",
                "--mouse-action", "move",
                "--x", "10",
                "--y", "20",
                "--monitor-index", "1",
                "--key", "65",
                "--key-down",
                "--hidden",
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
            Assert.AreEqual("Agent", options.Name);
            Assert.AreEqual("Renamed", options.NewName);
            Assert.AreEqual("current-user-run", options.StartupType);
            Assert.AreEqual(1234, options.Pid);
            Assert.AreEqual("restart", options.Action);
            Assert.AreEqual("Notice", options.Caption);
            Assert.AreEqual("Hello", options.Text);
            Assert.AreEqual("OKCancel", options.Button);
            Assert.AreEqual("Information", options.Icon);
            Assert.AreEqual("https://example.test", options.Url);
            Assert.AreEqual(80, options.Quality);
            Assert.AreEqual(2, options.DisplayIndex);
            Assert.AreEqual("move", options.MouseAction);
            Assert.AreEqual(10, options.X);
            Assert.AreEqual(20, options.Y);
            Assert.AreEqual(1, options.MonitorIndex);
            Assert.AreEqual((byte)65, options.Key);
            Assert.AreEqual(true, options.KeyDown);
            Assert.IsTrue(options.Hidden);
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
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "get-desktop", "--quality", "0" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "get-desktop", "--display-index", "-1" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "mouse-event", "--x", "10", "--y", "20", "--monitor-index", "0" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "keyboard-event", "--key", "65" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "keyboard-event", "--key", "65", "--key-down", "--key-up" }));
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
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "get-monitors" })),
                typeof(GetMonitors));

            var desktop = (GetDesktop)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "get-desktop",
                "--quality", "80",
                "--display-index", "1"
            }));
            Assert.IsTrue(desktop.CreateNew);
            Assert.AreEqual(80, desktop.Quality);
            Assert.AreEqual(1, desktop.DisplayIndex);

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

            var registryCreateKey = (DoCreateRegistryKey)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-create-key",
                "--path", "HKCU\\Software"
            }));
            Assert.AreEqual("HKCU\\Software", registryCreateKey.ParentPath);

            var registryDeleteKey = (DoDeleteRegistryKey)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-delete-key",
                "--path", "HKCU\\Software",
                "--name", "Old"
            }));
            Assert.AreEqual("HKCU\\Software", registryDeleteKey.ParentPath);
            Assert.AreEqual("Old", registryDeleteKey.KeyName);

            var registryRenameKey = (DoRenameRegistryKey)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-rename-key",
                "--path", "HKCU\\Software",
                "--name", "Old",
                "--new-name", "New"
            }));
            Assert.AreEqual("HKCU\\Software", registryRenameKey.ParentPath);
            Assert.AreEqual("Old", registryRenameKey.OldKeyName);
            Assert.AreEqual("New", registryRenameKey.NewKeyName);

            var registryCreateValue = (DoCreateRegistryValue)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-create-value",
                "--path", "HKCU\\Software",
                "--kind", "string"
            }));
            Assert.AreEqual("HKCU\\Software", registryCreateValue.KeyPath);
            Assert.AreEqual(RegistryValueKind.String, registryCreateValue.Kind);

            var registryDeleteValue = (DoDeleteRegistryValue)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-delete-value",
                "--path", "HKCU\\Software",
                "--name", "Old"
            }));
            Assert.AreEqual("HKCU\\Software", registryDeleteValue.KeyPath);
            Assert.AreEqual("Old", registryDeleteValue.ValueName);

            var registryRenameValue = (DoRenameRegistryValue)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-rename-value",
                "--path", "HKCU\\Software",
                "--name", "Old",
                "--new-name", "New"
            }));
            Assert.AreEqual("HKCU\\Software", registryRenameValue.KeyPath);
            Assert.AreEqual("Old", registryRenameValue.OldValueName);
            Assert.AreEqual("New", registryRenameValue.NewValueName);

            var registryChangeValue = (DoChangeRegistryValue)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "registry-change-value",
                "--path", "HKCU\\Software",
                "--name", "Answer",
                "--kind", "dword",
                "--data", "42"
            }));
            Assert.AreEqual("HKCU\\Software", registryChangeValue.KeyPath);
            Assert.AreEqual("Answer", registryChangeValue.Value.Name);
            Assert.AreEqual(RegistryValueKind.DWord, registryChangeValue.Value.Kind);
            CollectionAssert.AreEqual(BitConverter.GetBytes((uint)42), registryChangeValue.Value.Data);

            var shellExecute = (DoShellExecute)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "shell-execute",
                "--shell-command", "whoami"
            }));
            Assert.AreEqual("whoami", shellExecute.Command);

            var mouseEvent = (DoMouseEvent)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "mouse-event",
                "--mouse-action", "left-down",
                "--x", "10",
                "--y", "20",
                "--monitor-index", "1"
            }));
            Assert.AreEqual(MouseAction.LeftDown, mouseEvent.Action);
            Assert.IsTrue(mouseEvent.IsMouseDown);
            Assert.AreEqual(10, mouseEvent.X);
            Assert.AreEqual(20, mouseEvent.Y);
            Assert.AreEqual(1, mouseEvent.MonitorIndex);

            var keyboardEvent = (DoKeyboardEvent)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "keyboard-event",
                "--key", "65",
                "--key-up"
            }));
            Assert.AreEqual((byte)65, keyboardEvent.Key);
            Assert.IsFalse(keyboardEvent.KeyDown);

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

            var deleteDirectory = (DoPathDelete)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "delete-path",
                "--path", "C:\\Temp\\old-folder",
                "--type", "directory"
            }));
            Assert.AreEqual("C:\\Temp\\old-folder", deleteDirectory.Path);
            Assert.AreEqual(FileType.Directory, deleteDirectory.PathType);

            var startupAdd = (DoStartupItemAdd)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "startup-add",
                "--name", "Agent",
                "--path", "C:\\Tools\\agent.exe",
                "--startup-type", "current-user-run"
            }));
            Assert.AreEqual("Agent", startupAdd.StartupItem.Name);
            Assert.AreEqual("C:\\Tools\\agent.exe", startupAdd.StartupItem.Path);
            Assert.AreEqual(StartupType.CurrentUserRun, startupAdd.StartupItem.Type);

            var startupRemove = (DoStartupItemRemove)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "startup-remove",
                "--name", "Agent",
                "--startup-type", "current-user-run-once"
            }));
            Assert.AreEqual("Agent", startupRemove.StartupItem.Name);
            Assert.AreEqual(StartupType.CurrentUserRunOnce, startupRemove.StartupItem.Type);

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
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "disconnect-client" })),
                typeof(DoClientDisconnect));
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "reconnect-client" })),
                typeof(DoClientReconnect));
            Assert.IsInstanceOfType(
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "uninstall-client" })),
                typeof(DoClientUninstall));

            var shutdownAction = (DoShutdownAction)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "shutdown-action",
                "--action", "restart"
            }));
            Assert.AreEqual(ShutdownAction.Restart, shutdownAction.Action);

            var messageBox = (DoShowMessageBox)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "show-message",
                "--caption", "Notice",
                "--text", "Hello",
                "--button", "OKCancel",
                "--icon", "Information"
            }));
            Assert.AreEqual("Notice", messageBox.Caption);
            Assert.AreEqual("Hello", messageBox.Text);
            Assert.AreEqual("OKCancel", messageBox.Button);
            Assert.AreEqual("Information", messageBox.Icon);

            var visitWebsite = (DoVisitWebsite)Program.CreateMessage(CliOptions.Parse(new[]
            {
                "dispatch",
                "--command", "visit-website",
                "--url", "example.test",
                "--hidden"
            }));
            Assert.AreEqual("http://example.test/", visitWebsite.Url);
            Assert.IsTrue(visitWebsite.Hidden);

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
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "show-message" }));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "show-message", "--text", "Hello", "--button", "Bogus" })));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "show-message", "--text", "Hello", "--icon", "Bogus" })));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "visit-website" }));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "visit-website", "--url", "file:///C:/Temp/a.txt" })));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-create-key" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-delete-key", "--path", "HKCU\\Software" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-rename-key", "--path", "HKCU\\Software", "--name", "Old" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-create-value", "--path", "HKCU\\Software" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-delete-value", "--path", "HKCU\\Software" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-rename-value", "--path", "HKCU\\Software", "--name", "Old" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "registry-change-value", "--path", "HKCU\\Software", "--name", "Answer", "--kind", "dword" }));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "registry-change-value", "--path", "HKCU\\Software", "--name", "Answer", "--kind", "bogus", "--data", "42" })));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "shell-execute" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "startup-add", "--name", "Agent", "--startup-type", "current-user-run" }));
            Assert.ThrowsException<ArgumentException>(() =>
                CliOptions.Parse(new[] { "dispatch", "--command", "startup-remove", "--startup-type", "current-user-run" }));
            Assert.ThrowsException<ArgumentException>(() =>
                Program.CreateMessage(CliOptions.Parse(new[] { "dispatch", "--command", "startup-remove", "--name", "Agent", "--startup-type", "bogus" })));
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

            ListenCommand startupAdd = ListenCommand.Parse("dispatch first startup-add --name Agent --path C:\\Tools\\agent.exe --startup-type current-user-run");
            Assert.AreEqual("startup-add", startupAdd.DispatchCommand);
            Assert.AreEqual("Agent", startupAdd.Name);
            Assert.AreEqual("C:\\Tools\\agent.exe", startupAdd.Path);
            Assert.AreEqual("current-user-run", startupAdd.StartupType);

            ListenCommand startupRemove = ListenCommand.Parse("dispatch first startup-remove --name Agent --startup-type current-user-run-once");
            Assert.AreEqual("startup-remove", startupRemove.DispatchCommand);
            Assert.AreEqual("Agent", startupRemove.Name);
            Assert.AreEqual("current-user-run-once", startupRemove.StartupType);

            ListenCommand endProcess = ListenCommand.Parse("dispatch first end-process --pid 4321");
            Assert.AreEqual("end-process", endProcess.DispatchCommand);
            Assert.AreEqual(4321, endProcess.Pid);

            ListenCommand askElevate = ListenCommand.Parse("dispatch first ask-elevate");
            Assert.AreEqual("ask-elevate", askElevate.DispatchCommand);

            ListenCommand disconnectClient = ListenCommand.Parse("dispatch first disconnect-client");
            Assert.AreEqual("disconnect-client", disconnectClient.DispatchCommand);

            ListenCommand reconnectClient = ListenCommand.Parse("dispatch first reconnect-client");
            Assert.AreEqual("reconnect-client", reconnectClient.DispatchCommand);

            ListenCommand uninstallClient = ListenCommand.Parse("dispatch first uninstall-client");
            Assert.AreEqual("uninstall-client", uninstallClient.DispatchCommand);

            ListenCommand monitors = ListenCommand.Parse("dispatch first get-monitors");
            Assert.AreEqual("get-monitors", monitors.DispatchCommand);

            ListenCommand desktop = ListenCommand.Parse("dispatch first get-desktop --quality 80 --display-index 1 --output C:\\Temp\\desktop.jpg");
            Assert.AreEqual("get-desktop", desktop.DispatchCommand);
            Assert.AreEqual(80, desktop.Quality);
            Assert.AreEqual(1, desktop.DisplayIndex);
            Assert.AreEqual("C:\\Temp\\desktop.jpg", desktop.OutputPath);

            ListenCommand mouseEvent = ListenCommand.Parse("dispatch first mouse-event --mouse-action move --x 10 --y 20 --monitor-index 1");
            Assert.AreEqual("mouse-event", mouseEvent.DispatchCommand);
            Assert.AreEqual("move", mouseEvent.MouseAction);
            Assert.AreEqual(10, mouseEvent.X);
            Assert.AreEqual(20, mouseEvent.Y);
            Assert.AreEqual(1, mouseEvent.MonitorIndex);

            ListenCommand keyboardEvent = ListenCommand.Parse("dispatch first keyboard-event --key 65 --key-down");
            Assert.AreEqual("keyboard-event", keyboardEvent.DispatchCommand);
            Assert.AreEqual((byte)65, keyboardEvent.Key);
            Assert.AreEqual(true, keyboardEvent.KeyDown);

            ListenCommand shutdownAction = ListenCommand.Parse("dispatch first shutdown-action --action standby");
            Assert.AreEqual("shutdown-action", shutdownAction.DispatchCommand);
            Assert.AreEqual("standby", shutdownAction.Action);

            ListenCommand messageBox = ListenCommand.Parse("dispatch first show-message --caption Notice --text Hello --button OK --icon Information");
            Assert.AreEqual("show-message", messageBox.DispatchCommand);
            Assert.AreEqual("Notice", messageBox.Caption);
            Assert.AreEqual("Hello", messageBox.Text);
            Assert.AreEqual("OK", messageBox.Button);
            Assert.AreEqual("Information", messageBox.Icon);

            ListenCommand visitWebsite = ListenCommand.Parse("dispatch first visit-website --url example.test --hidden");
            Assert.AreEqual("visit-website", visitWebsite.DispatchCommand);
            Assert.AreEqual("example.test", visitWebsite.Url);
            Assert.IsTrue(visitWebsite.Hidden);

            ListenCommand startProcess = ListenCommand.Parse("dispatch first start-process --path C:\\Temp\\run.cmd");
            Assert.AreEqual("start-process", startProcess.DispatchCommand);
            Assert.AreEqual("C:\\Temp\\run.cmd", startProcess.Path);

            ListenCommand registryKey = ListenCommand.Parse("dispatch first get-registry-key --path HKCU\\Software");
            Assert.AreEqual("get-registry-key", registryKey.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryKey.Path);

            ListenCommand registryCreateKey = ListenCommand.Parse("dispatch first registry-create-key --path HKCU\\Software");
            Assert.AreEqual("registry-create-key", registryCreateKey.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryCreateKey.Path);

            ListenCommand registryDeleteKey = ListenCommand.Parse("dispatch first registry-delete-key --path HKCU\\Software --name Old");
            Assert.AreEqual("registry-delete-key", registryDeleteKey.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryDeleteKey.Path);
            Assert.AreEqual("Old", registryDeleteKey.Name);

            ListenCommand registryRenameKey = ListenCommand.Parse("dispatch first registry-rename-key --path HKCU\\Software --name Old --new-name New");
            Assert.AreEqual("registry-rename-key", registryRenameKey.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryRenameKey.Path);
            Assert.AreEqual("Old", registryRenameKey.Name);
            Assert.AreEqual("New", registryRenameKey.NewName);

            ListenCommand registryCreateValue = ListenCommand.Parse("dispatch first registry-create-value --path HKCU\\Software --kind string");
            Assert.AreEqual("registry-create-value", registryCreateValue.DispatchCommand);
            Assert.AreEqual("HKCU\\Software", registryCreateValue.Path);
            Assert.AreEqual("string", registryCreateValue.Kind);

            ListenCommand registryChangeValue = ListenCommand.Parse("dispatch first registry-change-value --path HKCU\\Software --name Answer --kind dword --data 42");
            Assert.AreEqual("registry-change-value", registryChangeValue.DispatchCommand);
            Assert.AreEqual("Answer", registryChangeValue.Name);
            Assert.AreEqual("dword", registryChangeValue.Kind);
            Assert.AreEqual("42", registryChangeValue.Data);

            ListenCommand shellExecute = ListenCommand.Parse("dispatch first shell-execute --shell-command whoami");
            Assert.AreEqual("shell-execute", shellExecute.DispatchCommand);
            Assert.AreEqual("whoami", shellExecute.ShellCommand);

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
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first startup-add --name Agent --startup-type current-user-run"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first startup-remove --startup-type current-user-run"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first end-process"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first shutdown-action"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first show-message"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first visit-website"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first start-process"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first get-registry-key"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-create-key"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-delete-key --path HKCU\\Software"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-rename-key --path HKCU\\Software --name Old"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-create-value --path HKCU\\Software"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-delete-value --path HKCU\\Software"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-rename-value --path HKCU\\Software --name Old"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first registry-change-value --path HKCU\\Software --name Answer --kind dword"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first shell-execute"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first get-desktop --quality 101"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first get-desktop --display-index -1"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first mouse-event --x 10 --y 20 --monitor-index 0"));
            Assert.ThrowsException<ArgumentException>(() => ListenCommand.Parse("dispatch first keyboard-event --key 65"));
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
                new[] { "Monitors: 2." },
                Program.FormatResponse(new GetMonitorsResponse { Number = 2 }));

            CollectionAssert.AreEqual(
                new[] { "Desktop frame: Monitor=1; Quality=80; Resolution=640x480; ImageBytes=4." },
                Program.FormatResponse(new GetDesktopResponse
                {
                    Monitor = 1,
                    Quality = 80,
                    Resolution = new MasterSplinter.Common.Video.Resolution { Width = 640, Height = 480 },
                    Image = new byte[] { 1, 2, 3, 4 }
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
                new[] { "Registry create key: Parent=HKCU\\Software; Key=New Key #1; IsError=False; Error=-." },
                Program.FormatResponse(new GetCreateRegistryKeyResponse
                {
                    ParentPath = "HKCU\\Software",
                    Match = new RegSeekerMatch { Key = "New Key #1" },
                    IsError = false
                }));

            CollectionAssert.AreEqual(
                new[] { "Registry delete key: Parent=HKCU\\Software; Key=Old; IsError=True; Error=Denied." },
                Program.FormatResponse(new GetDeleteRegistryKeyResponse
                {
                    ParentPath = "HKCU\\Software",
                    KeyName = "Old",
                    IsError = true,
                    ErrorMsg = "Denied"
                }));

            CollectionAssert.AreEqual(
                new[] { "Registry rename key: Parent=HKCU\\Software; OldKey=Old; NewKey=New; IsError=False; Error=-." },
                Program.FormatResponse(new GetRenameRegistryKeyResponse
                {
                    ParentPath = "HKCU\\Software",
                    OldKeyName = "Old",
                    NewKeyName = "New"
                }));

            CollectionAssert.AreEqual(
                new[] { "Registry create value: Key=HKCU\\Software; Value=New Value #1; Kind=String; IsError=False; Error=-." },
                Program.FormatResponse(new GetCreateRegistryValueResponse
                {
                    KeyPath = "HKCU\\Software",
                    Value = new RegValueData { Name = "New Value #1", Kind = RegistryValueKind.String }
                }));

            CollectionAssert.AreEqual(
                new[] { "Registry delete value: Key=HKCU\\Software; Value=Old; IsError=True; Error=Denied." },
                Program.FormatResponse(new GetDeleteRegistryValueResponse
                {
                    KeyPath = "HKCU\\Software",
                    ValueName = "Old",
                    IsError = true,
                    ErrorMsg = "Denied"
                }));

            CollectionAssert.AreEqual(
                new[] { "Registry rename value: Key=HKCU\\Software; OldValue=Old; NewValue=New; IsError=False; Error=-." },
                Program.FormatResponse(new GetRenameRegistryValueResponse
                {
                    KeyPath = "HKCU\\Software",
                    OldValueName = "Old",
                    NewValueName = "New"
                }));

            CollectionAssert.AreEqual(
                new[] { "Registry change value: Key=HKCU\\Software; Value=Answer; Kind=DWord; IsError=False; Error=-." },
                Program.FormatResponse(new GetChangeRegistryValueResponse
                {
                    KeyPath = "HKCU\\Software",
                    Value = new RegValueData { Name = "Answer", Kind = RegistryValueKind.DWord }
                }));

            CollectionAssert.AreEqual(
                new[] { "Shell response: IsError=False; Output=hello" },
                Program.FormatResponse(new DoShellExecuteResponse
                {
                    Output = "hello"
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
                new[] { "User status: Idle" },
                Program.FormatResponse(new SetUserStatus
                {
                    Message = UserStatus.Idle
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
#pragma warning restore CA1416
}
