using System;
using System.Collections.Generic;

namespace MasterSplinter.Cli
{
    public sealed class ListenCommand
    {
        private ListenCommand(string verb, string clientId, string dispatchCommand, string path)
        {
            Verb = verb;
            ClientId = clientId;
            DispatchCommand = dispatchCommand;
            Path = path;
        }

        public string Verb { get; }
        public string ClientId { get; }
        public string DispatchCommand { get; }
        public string Path { get; }

        public static ListenCommand Parse(string line)
        {
            string[] tokens = Tokenize(line);
            if (tokens.Length == 0)
                return new ListenCommand("empty", null, null, null);

            string verb = tokens[0];
            if (string.Equals(verb, "exit", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(verb, "quit", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("exit", null, null, null);
            if (string.Equals(verb, "help", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("help", null, null, null);
            if (string.Equals(verb, "clients", StringComparison.OrdinalIgnoreCase))
                return new ListenCommand("clients", null, null, null);

            if (!string.Equals(verb, "dispatch", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Unknown listen command '{verb}'.");
            if (tokens.Length < 3)
                throw new ArgumentException("Usage: dispatch <client-id|first> <command> [--path <path>]");

            string clientId = tokens[1];
            string dispatchCommand = tokens[2];
            string path = null;
            for (int index = 3; index < tokens.Length; index++)
            {
                if (string.Equals(tokens[index], "--path", StringComparison.OrdinalIgnoreCase))
                {
                    index++;
                    if (index >= tokens.Length || string.IsNullOrWhiteSpace(tokens[index]))
                        throw new ArgumentException("--path requires a value.");

                    path = tokens[index];
                }
                else
                {
                    throw new ArgumentException($"Unknown dispatch argument '{tokens[index]}'.");
                }
            }

            if (string.Equals(dispatchCommand, "get-directory", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("--path is required for get-directory.");

            return new ListenCommand("dispatch", clientId, dispatchCommand, path);
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
