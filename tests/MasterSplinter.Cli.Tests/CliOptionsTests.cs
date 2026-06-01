using System;
using MasterSplinter.Cli;
using Quasar.Common.Messages;

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
    }
}
