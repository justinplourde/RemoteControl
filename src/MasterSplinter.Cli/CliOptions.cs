using System;
using System.Globalization;

namespace MasterSplinter.Cli
{
    public sealed class CliOptions
    {
        public const string Usage =
            "Usage: MasterSplinter.Cli <dispatch|listen> [--command <get-system-info|get-drives|get-directory|get-processes|get-startup-items|get-connections>] [--path <path>] [--host 127.0.0.1] [--port 4782] [--timeout-seconds 60] [--operator-id cli-operator] [--grant-permission] [--grant-consent]";

        private CliOptions(
            string command,
            string dispatchCommand,
            string path,
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
                return new CliOptions(null, null, null, "127.0.0.1", 4782, 60, "cli-operator", false, false, true);
            }

            string command = args[0];
            string dispatchCommand = null;
            string path = null;
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

            if (string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("--path is required for get-directory.");

            return new CliOptions(
                command,
                dispatchCommand,
                path,
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
