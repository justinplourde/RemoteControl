using System;
using System.Globalization;

namespace MasterSplinter.Cli
{
    public sealed class CliOptions
    {
        public const string Usage =
            "Usage: MasterSplinter.Cli <dispatch|listen> [--command <get-system-info|get-drives|get-directory|download-file|upload-file|rename-path|delete-path|start-process|end-process|get-processes|get-startup-items|get-connections>] [--path <path>] [--new-path <path>] [--type <file|directory>] [--pid <pid>] [--remote-path <client-path>] [--output <local-path>] [--host 127.0.0.1] [--port 4782] [--timeout-seconds 60] [--operator-id cli-operator] [--grant-permission] [--grant-consent]";

        private CliOptions(
            string command,
            string dispatchCommand,
            string path,
            string newPath,
            string pathType,
            int? pid,
            string remotePath,
            string outputPath,
            string host,
            int port,
            int timeoutSeconds,
            string operatorId,
            bool grantPermission,
            bool grantConsent,
            bool showHelp)
        {
            Command = command;
            DispatchCommand = dispatchCommand;
            Path = path;
            NewPath = newPath;
            PathType = pathType;
            Pid = pid;
            RemotePath = remotePath;
            OutputPath = outputPath;
            Host = host;
            Port = port;
            TimeoutSeconds = timeoutSeconds;
            OperatorId = operatorId;
            GrantPermission = grantPermission;
            GrantConsent = grantConsent;
            ShowHelp = showHelp;
        }

        public string Command { get; }
        public string DispatchCommand { get; }
        public string Path { get; }
        public string NewPath { get; }
        public string PathType { get; }
        public int? Pid { get; }
        public string RemotePath { get; }
        public string OutputPath { get; }
        public string Host { get; }
        public int Port { get; }
        public int TimeoutSeconds { get; }
        public string OperatorId { get; }
        public bool GrantPermission { get; }
        public bool GrantConsent { get; }
        public bool ShowHelp { get; }

        public static CliOptions Parse(string[] args)
        {
            if (args == null || args.Length == 0 ||
                string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase))
            {
                return new CliOptions(null, null, null, null, null, null, null, null, "127.0.0.1", 4782, 60, "cli-operator", false, false, true);
            }

            string command = args[0];
            string dispatchCommand = null;
            string path = null;
            string newPath = null;
            string pathType = null;
            int? pid = null;
            string remotePath = null;
            string outputPath = null;
            string host = "127.0.0.1";
            int port = 4782;
            int timeoutSeconds = 60;
            string operatorId = "cli-operator";
            bool grantPermission = false;
            bool grantConsent = false;

            for (int index = 1; index < args.Length; index++)
            {
                string arg = args[index];
                if (string.Equals(arg, "--command", StringComparison.OrdinalIgnoreCase))
                    dispatchCommand = ReadValue(args, ref index, "--command");
                else if (string.Equals(arg, "--path", StringComparison.OrdinalIgnoreCase))
                    path = ReadValue(args, ref index, "--path");
                else if (string.Equals(arg, "--new-path", StringComparison.OrdinalIgnoreCase))
                    newPath = ReadValue(args, ref index, "--new-path");
                else if (string.Equals(arg, "--type", StringComparison.OrdinalIgnoreCase))
                    pathType = ReadValue(args, ref index, "--type");
                else if (string.Equals(arg, "--pid", StringComparison.OrdinalIgnoreCase))
                    pid = int.Parse(ReadValue(args, ref index, "--pid"), CultureInfo.InvariantCulture);
                else if (string.Equals(arg, "--remote-path", StringComparison.OrdinalIgnoreCase))
                    remotePath = ReadValue(args, ref index, "--remote-path");
                else if (string.Equals(arg, "--output", StringComparison.OrdinalIgnoreCase))
                    outputPath = ReadValue(args, ref index, "--output");
                else if (string.Equals(arg, "--host", StringComparison.OrdinalIgnoreCase))
                    host = ReadValue(args, ref index, "--host");
                else if (string.Equals(arg, "--port", StringComparison.OrdinalIgnoreCase))
                    port = int.Parse(ReadValue(args, ref index, "--port"), CultureInfo.InvariantCulture);
                else if (string.Equals(arg, "--timeout-seconds", StringComparison.OrdinalIgnoreCase))
                    timeoutSeconds = int.Parse(ReadValue(args, ref index, "--timeout-seconds"), CultureInfo.InvariantCulture);
                else if (string.Equals(arg, "--operator-id", StringComparison.OrdinalIgnoreCase))
                    operatorId = ReadValue(args, ref index, "--operator-id");
                else if (string.Equals(arg, "--grant-permission", StringComparison.OrdinalIgnoreCase))
                    grantPermission = true;
                else if (string.Equals(arg, "--grant-consent", StringComparison.OrdinalIgnoreCase))
                    grantConsent = true;
                else
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }

            if (!string.Equals(command, "dispatch", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(command, "listen", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Unknown command '{command}'.");

            if (string.Equals(command, "dispatch", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(dispatchCommand))
                throw new ArgumentException("--command is required for dispatch.");

            if ((string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "start-process", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "download-file", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(path))
                throw new ArgumentException($"--path is required for {dispatchCommand}.");

            if (string.Equals(dispatchCommand, "upload-file", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for upload-file.");
                if (string.IsNullOrWhiteSpace(remotePath))
                    throw new ArgumentException("--remote-path is required for upload-file.");
            }

            if (string.Equals(dispatchCommand, "rename-path", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for rename-path.");
                if (string.IsNullOrWhiteSpace(newPath))
                    throw new ArgumentException("--new-path is required for rename-path.");
                if (string.IsNullOrWhiteSpace(pathType))
                    throw new ArgumentException("--type is required for rename-path.");
            }

            if (string.Equals(dispatchCommand, "delete-path", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for delete-path.");
                if (string.IsNullOrWhiteSpace(pathType))
                    throw new ArgumentException("--type is required for delete-path.");
            }

            if (string.Equals(dispatchCommand, "end-process", StringComparison.OrdinalIgnoreCase) &&
                !pid.HasValue)
                throw new ArgumentException("--pid is required for end-process.");

            return new CliOptions(
                command,
                dispatchCommand,
                path,
                newPath,
                pathType,
                pid,
                remotePath,
                outputPath,
                host,
                port,
                timeoutSeconds,
                operatorId,
                grantPermission,
                grantConsent,
                false);
        }

        private static string ReadValue(string[] args, ref int index, string optionName)
        {
            int valueIndex = index + 1;
            if (valueIndex >= args.Length || string.IsNullOrWhiteSpace(args[valueIndex]))
                throw new ArgumentException($"{optionName} requires a value.");

            index = valueIndex;
            return args[valueIndex];
        }
    }
}
