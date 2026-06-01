using System;
using System.Globalization;

namespace MasterSplinter.Client.Host
{
    internal sealed class HostOptions
    {
        private HostOptions(
            string host,
            int port,
            string clientId,
            string tag,
            string encryptionKey,
            byte[] signature,
            bool smokeTest)
        {
            Host = host;
            Port = port;
            ClientId = clientId;
            Tag = tag;
            EncryptionKey = encryptionKey;
            Signature = signature;
            SmokeTest = smokeTest;
        }

        public string Host { get; }

        public int Port { get; }

        public string ClientId { get; }

        public string Tag { get; }

        public string EncryptionKey { get; }

        public byte[] Signature { get; }

        public bool SmokeTest { get; }

        public static HostOptions Parse(string[] args)
        {
            string host = "127.0.0.1";
            int port = 4782;
            string clientId = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            string tag = "modern";
            string encryptionKey = "dev-key";
            byte[] signature = new byte[] { 1, 2, 3, 4 };
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
                else if (string.Equals(arg, "--client-id", StringComparison.OrdinalIgnoreCase))
                {
                    clientId = ReadValue(args, ref index, "--client-id");
                }
                else if (string.Equals(arg, "--tag", StringComparison.OrdinalIgnoreCase))
                {
                    tag = ReadValue(args, ref index, "--tag");
                }
                else if (string.Equals(arg, "--encryption-key", StringComparison.OrdinalIgnoreCase))
                {
                    encryptionKey = ReadValue(args, ref index, "--encryption-key");
                }
                else if (string.Equals(arg, "--smoke-test", StringComparison.OrdinalIgnoreCase))
                {
                    smokeTest = true;
                }
                else if (string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Usage: MasterSplinter.Client.Host [--host 127.0.0.1] [--port 4782] [--client-id <64 chars>] [--tag modern] [--encryption-key dev-key] [--smoke-test]");
                }
                else
                {
                    throw new ArgumentException($"Unknown argument '{arg}'.");
                }
            }

            return new HostOptions(host, port, clientId, tag, encryptionKey, signature, smokeTest);
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
