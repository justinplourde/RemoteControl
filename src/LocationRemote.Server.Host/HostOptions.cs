using System;
using System.Globalization;

namespace LocationRemote.Server.Host
{
    internal sealed class HostOptions
    {
        private HostOptions(string host, int port, bool smokeTest)
        {
            Host = host;
            Port = port;
            SmokeTest = smokeTest;
        }

        public string Host { get; }

        public int Port { get; }

        public bool SmokeTest { get; }

        public static HostOptions Parse(string[] args)
        {
            string host = "127.0.0.1";
            int port = 4782;
            bool smokeTest = false;

            for (int index = 0; index < args.Length; index++)
            {
                string arg = args[index];
                if (string.Equals(arg, "--host", StringComparison.OrdinalIgnoreCase))
                {
                    host = ReadValue(args, ref index, "--host");
                }
                else if (string.Equals(arg, "--port", StringComparison.OrdinalIgnoreCase))
                {
                    string value = ReadValue(args, ref index, "--port");
                    port = int.Parse(value, CultureInfo.InvariantCulture);
                }
                else if (string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase))
                {
                    smokeTest = true;
                }
                else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Usage: LocationRemote.Server.Host [--host 127.0.0.1] [--port 4782] [--smoke-test]");
                }
                else
                {
                    throw new ArgumentException($"Unknown argument '{arg}'.");
                }
            }

            return new HostOptions(host, port, smokeTest);
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
