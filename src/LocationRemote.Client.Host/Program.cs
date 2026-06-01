using LocationRemote.Client.Core.Identity;
using LocationRemote.Client.Host;
using Quasar.Common.Messages;
using Quasar.Common.Protocol;
using System;
using System.Threading;

namespace LocationRemote.Client.Host
{
    internal static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                HostOptions options = HostOptions.Parse(args);
                var identityOptions = new ClientIdentityOptions(
                    "modern-dev",
                    Environment.OSVersion.VersionString,
                    Environment.UserInteractive ? "User" : "Service",
                    "Unknown",
                    "XX",
                    0,
                    options.ClientId,
                    Environment.UserName,
                    Environment.MachineName,
                    options.Tag,
                    options.EncryptionKey,
                    options.Signature,
                    new ProtocolVersion { Major = 1, Minor = 0 },
                    new ClientCapabilities());

                identityOptions.Capabilities.SupportedFeatures.Add("handshake");
                identityOptions.Capabilities.SupportedFeatures.Add("message.dispatch");

                ClientIdentification identification = new ClientIdentificationFactory().Create(identityOptions);

                Console.WriteLine($"LocationRemote client host prepared identification for {identification.Id}.");

                if (options.SmokeTest)
                {
                    Console.WriteLine($"Target placeholder: {options.Host}:{options.Port}.");
                    Console.WriteLine("Transport not opened in smoke-test mode.");
                    Console.WriteLine("Smoke test completed.");
                    return 0;
                }

                ClientIdentificationResult result = new LoopbackTcpHandshakeClient()
                    .IdentifyAsync(options.Host, options.Port, identification, CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();

                Console.WriteLine($"Handshake result: {result.Result}.");
                return result.Result ? 0 : 2;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }
    }
}
