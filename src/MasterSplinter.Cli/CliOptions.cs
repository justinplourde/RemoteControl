using System;
using System.Globalization;

namespace MasterSplinter.Cli
{
    public sealed class CliOptions
    {
        public const string Usage =
            "Usage: MasterSplinter.Cli <dispatch|listen> [--command <get-system-info|get-drives|get-directory|get-registry-key|registry-create-key|registry-delete-key|registry-rename-key|download-file|upload-file|rename-path|delete-path|start-process|end-process|ask-elevate|shutdown-action|disconnect-client|reconnect-client|show-message|visit-website|startup-add|startup-remove|close-connection|get-processes|get-startup-items|get-connections>] [--path <path>] [--new-path <path>] [--type <file|directory>] [--name <name>] [--new-name <name>] [--startup-type <type>] [--pid <pid>] [--action <shutdown|restart|standby>] [--caption <title>] [--text <message>] [--button <AbortRetryIgnore|OK|OKCancel|RetryCancel|YesNo|YesNoCancel>] [--icon <None|Error|Hand|Question|Exclamation|Warning|Information|Asterisk>] [--url <http-url>] [--hidden] [--local-address <ip>] [--local-port <port>] [--remote-address <ip>] [--remote-port <port>] [--remote-path <client-path>] [--output <local-path>] [--host 127.0.0.1] [--port 4782] [--timeout-seconds 60] [--operator-id cli-operator] [--grant-permission] [--grant-consent]";

        private CliOptions(
            string command,
            string dispatchCommand,
            string path,
            string newPath,
            string pathType,
            string name,
            string newName,
            string startupType,
            int? pid,
            string action,
            string caption,
            string text,
            string button,
            string icon,
            string url,
            bool hidden,
            string localAddress,
            ushort? localPort,
            string remoteAddress,
            ushort? remotePort,
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
            Name = name;
            NewName = newName;
            StartupType = startupType;
            Pid = pid;
            Action = action;
            Caption = caption;
            Text = text;
            Button = button;
            Icon = icon;
            Url = url;
            Hidden = hidden;
            LocalAddress = localAddress;
            LocalPort = localPort;
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
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
        public string Name { get; }
        public string NewName { get; }
        public string StartupType { get; }
        public int? Pid { get; }
        public string Action { get; }
        public string Caption { get; }
        public string Text { get; }
        public string Button { get; }
        public string Icon { get; }
        public string Url { get; }
        public bool Hidden { get; }
        public string LocalAddress { get; }
        public ushort? LocalPort { get; }
        public string RemoteAddress { get; }
        public ushort? RemotePort { get; }
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
                return new CliOptions(null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null, "127.0.0.1", 4782, 60, "cli-operator", false, false, true);
            }

            string command = args[0];
            string dispatchCommand = null;
            string path = null;
            string newPath = null;
            string pathType = null;
            string name = null;
            string newName = null;
            string startupType = null;
            int? pid = null;
            string action = null;
            string caption = null;
            string text = null;
            string button = null;
            string icon = null;
            string url = null;
            bool hidden = false;
            string localAddress = null;
            ushort? localPort = null;
            string remoteAddress = null;
            ushort? remotePort = null;
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
                else if (string.Equals(arg, "--name", StringComparison.OrdinalIgnoreCase))
                    name = ReadValue(args, ref index, "--name");
                else if (string.Equals(arg, "--new-name", StringComparison.OrdinalIgnoreCase))
                    newName = ReadValue(args, ref index, "--new-name");
                else if (string.Equals(arg, "--startup-type", StringComparison.OrdinalIgnoreCase))
                    startupType = ReadValue(args, ref index, "--startup-type");
                else if (string.Equals(arg, "--pid", StringComparison.OrdinalIgnoreCase))
                    pid = int.Parse(ReadValue(args, ref index, "--pid"), CultureInfo.InvariantCulture);
                else if (string.Equals(arg, "--action", StringComparison.OrdinalIgnoreCase))
                    action = ReadValue(args, ref index, "--action");
                else if (string.Equals(arg, "--caption", StringComparison.OrdinalIgnoreCase))
                    caption = ReadValue(args, ref index, "--caption");
                else if (string.Equals(arg, "--text", StringComparison.OrdinalIgnoreCase))
                    text = ReadValue(args, ref index, "--text");
                else if (string.Equals(arg, "--button", StringComparison.OrdinalIgnoreCase))
                    button = ReadValue(args, ref index, "--button");
                else if (string.Equals(arg, "--icon", StringComparison.OrdinalIgnoreCase))
                    icon = ReadValue(args, ref index, "--icon");
                else if (string.Equals(arg, "--url", StringComparison.OrdinalIgnoreCase))
                    url = ReadValue(args, ref index, "--url");
                else if (string.Equals(arg, "--hidden", StringComparison.OrdinalIgnoreCase))
                    hidden = true;
                else if (string.Equals(arg, "--local-address", StringComparison.OrdinalIgnoreCase))
                    localAddress = ReadValue(args, ref index, "--local-address");
                else if (string.Equals(arg, "--local-port", StringComparison.OrdinalIgnoreCase))
                    localPort = ushort.Parse(ReadValue(args, ref index, "--local-port"), CultureInfo.InvariantCulture);
                else if (string.Equals(arg, "--remote-address", StringComparison.OrdinalIgnoreCase))
                    remoteAddress = ReadValue(args, ref index, "--remote-address");
                else if (string.Equals(arg, "--remote-port", StringComparison.OrdinalIgnoreCase))
                    remotePort = ushort.Parse(ReadValue(args, ref index, "--remote-port"), CultureInfo.InvariantCulture);
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
                string.Equals(dispatchCommand, "get-registry-key", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "registry-create-key", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "start-process", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "download-file", StringComparison.OrdinalIgnoreCase)) &&
                string.IsNullOrWhiteSpace(path))
                throw new ArgumentException($"--path is required for {dispatchCommand}.");

            if (string.Equals(dispatchCommand, "registry-delete-key", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for registry-delete-key.");
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for registry-delete-key.");
            }

            if (string.Equals(dispatchCommand, "registry-rename-key", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for registry-rename-key.");
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for registry-rename-key.");
                if (string.IsNullOrWhiteSpace(newName))
                    throw new ArgumentException("--new-name is required for registry-rename-key.");
            }

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

            if (string.Equals(dispatchCommand, "shutdown-action", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("--action is required for shutdown-action.");

            if (string.Equals(dispatchCommand, "show-message", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(text))
                    throw new ArgumentException("--text is required for show-message.");
            }

            if (string.Equals(dispatchCommand, "visit-website", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(url))
                throw new ArgumentException("--url is required for visit-website.");

            if (string.Equals(dispatchCommand, "startup-add", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for startup-add.");
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for startup-add.");
                if (string.IsNullOrWhiteSpace(startupType))
                    throw new ArgumentException("--startup-type is required for startup-add.");
            }

            if (string.Equals(dispatchCommand, "startup-remove", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for startup-remove.");
                if (string.IsNullOrWhiteSpace(startupType))
                    throw new ArgumentException("--startup-type is required for startup-remove.");
            }

            if (string.Equals(dispatchCommand, "close-connection", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(localAddress))
                    throw new ArgumentException("--local-address is required for close-connection.");
                if (!localPort.HasValue)
                    throw new ArgumentException("--local-port is required for close-connection.");
                if (string.IsNullOrWhiteSpace(remoteAddress))
                    throw new ArgumentException("--remote-address is required for close-connection.");
                if (!remotePort.HasValue)
                    throw new ArgumentException("--remote-port is required for close-connection.");
            }

            return new CliOptions(
                command,
                dispatchCommand,
                path,
                newPath,
                pathType,
                name,
                newName,
                startupType,
                pid,
                action,
                caption,
                text,
                button,
                icon,
                url,
                hidden,
                localAddress,
                localPort,
                remoteAddress,
                remotePort,
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
