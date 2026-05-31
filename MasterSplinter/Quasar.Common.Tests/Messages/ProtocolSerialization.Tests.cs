using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using Quasar.Common.Messages;
using Quasar.Common.Networking;
using System;
using System.IO;
using System.Linq;

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
                typeof(ClientIdentificationResult));
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
        public void TypeRegistryDiscoversModernMessageTypes()
        {
            var packetTypes = TypeRegistry.GetPacketTypes(typeof(IMessage)).ToArray();

            CollectionAssert.Contains(packetTypes, typeof(ClientIdentification));
            CollectionAssert.Contains(packetTypes, typeof(ClientIdentificationResult));
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
