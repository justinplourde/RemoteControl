using System;
using System.Globalization;

namespace MasterSplinter.Cli
{
    public sealed class CliOptions
    {
        public const string Usage =
            "Usage: MasterSplinter.Cli dispatch --command get-system-info [--host 127.0.0.1] [--port 4782] [--timeout-seconds 60]";

        private CliOptions(string command, string dispatchCommand, string host, int port, int timeoutSeconds, bool showHelp)
        {
            Command = command;
            DispatchCommand = dispatchCommand;
            Host = host;
            Port = port;
            TimeoutSeconds = timeoutSeconds;
            ShowHelp = showHelp;
        }

        public string Command { get; }
        public string DispatchCommand { get; }
        public string Host { get; }
        public int Port { get; }
        public int TimeoutSeconds { get; }
        public bool ShowHelp { get; }

        public static CliOptions Parse(string[] args)
        {
            if (args == null || args.Length == 0 ||
                string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase))
            {
                return new CliOptions(null, null, "127.0.0.1", 4782, 60, true);
            }

            string command = args[0];
            string dispatchCommand = null;
            string host = "127.0.0.1";
            int port = 4782;
            int timeoutSeconds = 60;

            for (int index = 1; index < args.Length; index++)
            {
                string arg = args[index];
                if (string.Equals(arg, "--command", StringComparison.OrdinalIgnoreCase))
                    dispatchCommand = ReadValue(args, ref index, "--command");
                else if (string.Equals(arg, "--host", StringComparison.OrdinalIgnoreCase))
                    host = ReadValue(args, ref index, "--host");
                else if (string.Equals(arg, "--port", StringComparison.OrdinalIgnoreCase))
                    port = int.Parse(ReadValue(args, ref index, "--port"), CultureInfo.InvariantCulture);
                else if (string.Equals(arg, "--timeout-seconds", StringComparison.OrdinalIgnoreCase))
                    timeoutSeconds = int.Parse(ReadValue(args, ref index, "--timeout-seconds"), CultureInfo.InvariantCulture);
                else
                    throw new ArgumentException($"Unknown argument '{arg}'.");
            }

            if (string.Equals(command, "dispatch", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(dispatchCommand))
                throw new ArgumentException("--command is required for dispatch.");

            return new CliOptions(command, dispatchCommand, host, port, timeoutSeconds, false);
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
