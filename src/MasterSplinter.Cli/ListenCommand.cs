using System;
using System.Collections.Generic;

namespace MasterSplinter.Cli
{
    public sealed class ListenCommand
    {
        private ListenCommand(
            string verb,
            string clientId,
            string dispatchCommand,
            string path,
            string newPath,
            string pathType,
            int? pid,
            string action,
            string caption,
            string text,
            string button,
            string icon,
            string localAddress,
            ushort? localPort,
            string remoteAddress,
            ushort? remotePort,
            string remotePath,
            string outputPath)
        {
            Verb = verb;
            ClientId = clientId;
            DispatchCommand = dispatchCommand;
            Path = path;
            NewPath = newPath;
            PathType = pathType;
            Pid = pid;
            Action = action;
            Caption = caption;
            Text = text;
            Button = button;
            Icon = icon;
            LocalAddress = localAddress;
            LocalPort = localPort;
            RemoteAddress = remoteAddress;
            RemotePort = remotePort;
            RemotePath = remotePath;
            OutputPath = outputPath;
        }

        public string Verb { get; }
        public string ClientId { get; }
        public string DispatchCommand { get; }
        public string Path { get; }
        public string NewPath { get; }
        public string PathType { get; }
        public int? Pid { get; }
        public string Action { get; }
        public string Caption { get; }
        public string Text { get; }
        public string Button { get; }
        public string Icon { get; }
        public string LocalAddress { get; }
        public ushort? LocalPort { get; }
        public string RemoteAddress { get; }
        public ushort? RemotePort { get; }
        public string RemotePath { get; }
        public string OutputPath { get; }

        public static ListenCommand Parse(string line)
        {
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0)
                return new ListenCommand("empty", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

            string verb = tokens[0];
            if (string.Equals(verb, "exit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(verb, "quit", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("exit", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            if (string.Equals(verb, "help", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("help", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
            if (string.Equals(verb, "clients", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("clients", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);

            if (!string.Equals(verb, "dispatch", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Unknown listen command '{verb}'.");
            if (tokens.Length < 3)
                throw new ArgumentException("Usage: dispatch <client-id|first> <command> [--path <path>] [--new-path <path>] [--type <file|directory>] [--pid <pid>] [--remote-path <client-path>] [--output <local-path>]");

            string clientId = tokens[1];
            string dispatchCommand = tokens[2];
            string path = null;
            string newPath = null;
            string pathType = null;
            int? pid = null;
            string action = null;
            string caption = null;
            string text = null;
            string button = null;
            string icon = null;
            string localAddress = null;
            ushort? localPort = null;
            string remoteAddress = null;
            ushort? remotePort = null;
            string remotePath = null;
            string outputPath = null;
            for (int index = 3; index < tokens.Length; index++)
            {
                if (string.Equals(tokens[index], "--path", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--path requires a value.");

                    path = tokens[index];
                }
                else if (string.Equals(tokens[index], "--new-path", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--new-path requires a value.");

                    newPath = tokens[index];
                }
                else if (string.Equals(tokens[index], "--type", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--type requires a value.");

                    pathType = tokens[index];
                }
                else if (string.Equals(tokens[index], "--pid", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--pid requires a value.");

                    pid = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--action", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--action requires a value.");

                    action = tokens[index];
                }
                else if (string.Equals(tokens[index], "--caption", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--caption requires a value.");

                    caption = tokens[index];
                }
                else if (string.Equals(tokens[index], "--text", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--text requires a value.");

                    text = tokens[index];
                }
                else if (string.Equals(tokens[index], "--button", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--button requires a value.");

                    button = tokens[index];
                }
                else if (string.Equals(tokens[index], "--icon", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--icon requires a value.");

                    icon = tokens[index];
                }
                else if (string.Equals(tokens[index], "--local-address", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--local-address requires a value.");

                    localAddress = tokens[index];
                }
                else if (string.Equals(tokens[index], "--local-port", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--local-port requires a value.");

                    localPort = ushort.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--remote-address", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--remote-address requires a value.");

                    remoteAddress = tokens[index];
                }
                else if (string.Equals(tokens[index], "--remote-port", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--remote-port requires a value.");

                    remotePort = ushort.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--remote-path", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--remote-path requires a value.");

                    remotePath = tokens[index];
                }
                else if (string.Equals(tokens[index], "--output", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--output requires a value.");

                    outputPath = tokens[index];
                }
                else
                {
                    throw new ArgumentException($"Unknown dispatch argument '{tokens[index]}'.");
                }
            }

            if ((string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "get-registry-key", StringComparison.OrdinalIgnoreCase) ||
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

            if (string.Equals(dispatchCommand, "shutdown-action", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("--action is required for shutdown-action.");

            if (string.Equals(dispatchCommand, "show-message", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("--text is required for show-message.");

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

            return new ListenCommand(
                "dispatch",
                clientId,
                dispatchCommand,
                path,
                newPath,
                pathType,
                pid,
                action,
                caption,
                text,
                button,
                icon,
                localAddress,
                localPort,
                remoteAddress,
                remotePort,
                remotePath,
                outputPath);
        }

        private static string[] Tokenize(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return Array.Empty<string>();

            var tokens = new List<string>();
            var current = new System.Text.StringBuilder();
            bool inQuotes = false;

            foreach (char character in line)
            {
                if (character == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (char.IsWhiteSpace(character) && !inQuotes)
                {
                    AddToken(tokens, current);
                    continue;
                }

                current.Append(character);
            }

            if (inQuotes)
                throw new ArgumentException("Unterminated quote in listen command.");

            AddToken(tokens, current);
            return tokens.ToArray();
        }

        private static void AddToken(List<string> tokens, System.Text.StringBuilder current)
        {
            if (current.Length == 0)
                return;

            tokens.Add(current.ToString());
            current.Clear();
        }
    }
}
