using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using Quasar.Common.Messages;
using Quasar.Common.Models;
using Quasar.Common.Networking;
using Quasar.Common.Protocol;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Quasar.Common.Tests.Messages
{
    [TestClass]
    public class ProtocolSerializationTests
    {
        [ClassInitialize]
        public static void Initialize(TestContext context)
        {
            TypeRegistry.AddTypesToSerializer(
                typeof(IMessage),
                typeof(ClientIdentification),
                typeof(ClientIdentificationResult),
                typeof(FileTransferRequest),
                typeof(FileTransferChunk),
                typeof(FileTransferComplete),
                typeof(FileTransferCancel));
        }

        [TestMethod, TestCategory("Protocol")]
        public void ClientIdentificationResultRoundTripsThroughProtobuf()
        {
            var message = new ClientIdentificationResult { Result = true };

            byte[] payload = Serialize(message);
            var roundTripped = Deserialize<ClientIdentificationResult>(payload);

            Assert.IsTrue(payload.Length > 0);
            Assert.IsTrue(roundTripped.Result);
        }

        [TestMethod, TestCategory("Protocol")]
        public void PayloadWriterPrefixesSerializedMessageLength()
        {
            var message = new ClientIdentificationResult { Result = true };

            using (var stream = new MemoryStream())
            {
                using (var writer = new PayloadWriter(stream, true))
                {
                    writer.WriteMessage(message);
                }

                byte[] payload = stream.ToArray();
                int lengthPrefix = BitConverter.ToInt32(payload, 0);

                Assert.AreEqual(payload.Length - sizeof(int), lengthPrefix);
            }
        }

        [TestMethod, TestCategory("Protocol")]
        public void PayloadReaderRoundTripsRegisteredInterfaceMessage()
        {
            var original = new ClientIdentification
            {
                Version = "1.4.1",
                OperatingSystem = "Windows 11 64 Bit",
                AccountType = "Admin",
                Country = "United States",
                CountryCode = "US",
                ImageIndex = 230,
                Id = new string('A', 64),
                Username = "jplou",
                PcName = "LOCATIONREMOTE",
                Tag = "modernization",
                EncryptionKey = "test-key",
                Signature = new byte[] { 1, 2, 3, 4 }
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = new PayloadWriter(stream, true))
                {
                    writer.WriteMessage(original);
                }

                using (var reader = new PayloadReader(stream.ToArray(), (int)stream.Length, false))
                {
                    var roundTripped = (ClientIdentification)reader.ReadMessage();

                    Assert.AreEqual(original.Version, roundTripped.Version);
                    Assert.AreEqual(original.OperatingSystem, roundTripped.OperatingSystem);
                    Assert.AreEqual(original.AccountType, roundTripped.AccountType);
                    Assert.AreEqual(original.Country, roundTripped.Country);
                    Assert.AreEqual(original.CountryCode, roundTripped.CountryCode);
                    Assert.AreEqual(original.ImageIndex, roundTripped.ImageIndex);
                    Assert.AreEqual(original.Id, roundTripped.Id);
                    Assert.AreEqual(original.Username, roundTripped.Username);
                    Assert.AreEqual(original.PcName, roundTripped.PcName);
                    Assert.AreEqual(original.Tag, roundTripped.Tag);
                    Assert.AreEqual(original.EncryptionKey, roundTripped.EncryptionKey);
                    CollectionAssert.AreEqual(original.Signature, roundTripped.Signature);
                }
            }
        }

        [TestMethod, TestCategory("Protocol")]
        public void LegacyClientIdentificationPayloadDefaultsProtocolMetadata()
        {
            var legacy = new ClientIdentification
            {
                Version = "1.4.1",
                OperatingSystem = "Windows 11 64 Bit",
                AccountType = "Admin",
                Country = "United States",
                CountryCode = "US",
                ImageIndex = 230,
                Id = new string('A', 64),
                Username = "jplou",
                PcName = "LOCATIONREMOTE",
                Tag = "legacy",
                EncryptionKey = "test-key",
                Signature = new byte[] { 1, 2, 3, 4 }
            };

            var roundTripped = Deserialize<ClientIdentification>(Serialize(legacy));

            Assert.IsNull(roundTripped.ProtocolVersion);
            Assert.IsNull(roundTripped.Capabilities);
        }

        [TestMethod, TestCategory("Protocol")]
        public void ClientIdentificationRoundTripsProtocolMetadata()
        {
            var original = new ClientIdentification
            {
                Version = "1.4.1",
                Id = new string('B', 64),
                EncryptionKey = "test-key",
                Signature = new byte[] { 4, 3, 2, 1 },
                ProtocolVersion = new ProtocolVersion { Major = 1, Minor = 0 },
                Capabilities = new ClientCapabilities
                {
                    SupportedFeatures = { "file-transfer", "protocol-versioning" }
                }
            };

            var roundTripped = Deserialize<ClientIdentification>(Serialize(original));

            Assert.AreEqual(1, roundTripped.ProtocolVersion.Major);
            Assert.AreEqual(0, roundTripped.ProtocolVersion.Minor);
            CollectionAssert.AreEqual(original.Capabilities.SupportedFeatures, roundTripped.Capabilities.SupportedFeatures);
        }

        [TestMethod, TestCategory("Protocol")]
        public void ClientIdentificationProtocolMetadataUsesAdditiveFieldNumbers()
        {
            Assert.AreEqual(100, GetProtoMemberTag(nameof(ClientIdentification.ProtocolVersion)));
            Assert.AreEqual(101, GetProtoMemberTag(nameof(ClientIdentification.Capabilities)));
        }

        [TestMethod, TestCategory("Protocol")]
        public void FileTransferMessagesRoundTripThroughRegisteredInterface()
        {
            IMessage[] messages =
            {
                new FileTransferRequest { Id = 7, RemotePath = "C:\\Temp\\report.pdf" },
                new FileTransferChunk
                {
                    Id = 7,
                    FilePath = "report.pdf",
                    FileSize = 3,
                    Chunk = new FileChunk { Offset = 0, Data = new byte[] { 1, 2, 3 } }
                },
                new FileTransferComplete { Id = 7, FilePath = "report.pdf" },
                new FileTransferCancel { Id = 7, Reason = "Cancelled" }
            };

            foreach (var message in messages)
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new PayloadWriter(stream, true))
                    {
                        writer.WriteMessage(message);
                    }

                    using (var reader = new PayloadReader(stream.ToArray(), (int)stream.Length, false))
                    {
                        Assert.AreEqual(message.GetType(), reader.ReadMessage().GetType());
                    }
                }
            }
        }

        [TestMethod, TestCategory("Protocol")]
        public void TypeRegistryDiscoversModernMessageTypes()
        {
            var packetTypes = TypeRegistry.GetPacketTypes(typeof(IMessage)).ToArray();

            CollectionAssert.Contains(packetTypes, typeof(ClientIdentification));
            CollectionAssert.Contains(packetTypes, typeof(ClientIdentificationResult));
            CollectionAssert.Contains(packetTypes, typeof(FileTransferRequest));
            CollectionAssert.Contains(packetTypes, typeof(FileTransferChunk));
            CollectionAssert.Contains(packetTypes, typeof(FileTransferComplete));
            CollectionAssert.Contains(packetTypes, typeof(FileTransferCancel));
        }

        private static int GetProtoMemberTag(string propertyName)
        {
            return typeof(ClientIdentification)
                .GetProperty(propertyName)
                .GetCustomAttribute<ProtoMemberAttribute>()
                .Tag;
        }

        private static byte[] Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, value);
                return stream.ToArray();
            }
        }

        private static T Deserialize<T>(byte[] value)
        {
            using (var stream = new MemoryStream(value))
            {
                return Serializer.Deserialize<T>(stream);
            }
        }
    }
}
