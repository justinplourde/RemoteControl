using System;
using System.Collections.Generic;

namespace MasterSplinter.Cli
{
    public sealed class ListenCommand
    {
        private ListenCommand(string verb, string clientId, string dispatchCommand, string path, string remotePath, string outputPath)
        {
            Verb = verb;
            ClientId = clientId;
            DispatchCommand = dispatchCommand;
            Path = path;
            RemotePath = remotePath;
            OutputPath = outputPath;
        }

        public string Verb { get; }
        public string ClientId { get; }
        public string DispatchCommand { get; }
        public string Path { get; }
        public string RemotePath { get; }
        public string OutputPath { get; }

        public static ListenCommand Parse(string line)
        {
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0)
                return new ListenCommand("empty", null, null, null, null, null);

            string verb = tokens[0];
            if (string.Equals(verb, "exit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(verb, "quit", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("exit", null, null, null, null, null);
            if (string.Equals(verb, "help", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("help", null, null, null, null, null);
            if (string.Equals(verb, "clients", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("clients", null, null, null, null, null);

            if (!string.Equals(verb, "dispatch", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Unknown listen command '{verb}'.");
            if (tokens.Length < 3)
                throw new ArgumentException("Usage: dispatch <client-id|first> <command> [--path <path>] [--remote-path <client-path>] [--output <local-path>]");

            string clientId = tokens[1];
            string dispatchCommand = tokens[2];
            string path = null;
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

            return new ListenCommand("dispatch", clientId, dispatchCommand, path, remotePath, outputPath);
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
