using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf.Meta;
using Quasar.Common.Enums;
using Quasar.Common.Messages;
using Quasar.Common.Models;
using System;
using System.IO;

namespace Quasar.Common.Tests.Messages
{
    [TestClass]
    public class WireCompatibilityTests
    {
        [TestMethod, TestCategory("Protocol")]
        public void LegacyClientIdentificationFixtureDeserializesWithoutProtocolMetadata()
        {
            var message = Deserialize<ClientIdentification>(Fixtures.ClientIdentification);

            Assert.AreEqual("1.4.1", message.Version);
            Assert.AreEqual("Windows 11 64 Bit", message.OperatingSystem);
            Assert.AreEqual("Admin", message.AccountType);
            Assert.AreEqual("United States", message.Country);
            Assert.AreEqual("US", message.CountryCode);
            Assert.AreEqual(230, message.ImageIndex);
            Assert.AreEqual(new string('A', 64), message.Id);
            Assert.AreEqual("jplou", message.Username);
            Assert.AreEqual("LOCATIONREMOTE", message.PcName);
            Assert.AreEqual("legacy", message.Tag);
            Assert.AreEqual("test-key", message.EncryptionKey);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, message.Signature);
            Assert.IsNull(message.ProtocolVersion);
            Assert.IsNull(message.Capabilities);
        }

        [TestMethod, TestCategory("Protocol")]
        public void ModernHandshakeContractsEmitPinnedWireFixtures()
        {
            AssertFixture(Fixtures.ClientIdentification, new ClientIdentification
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
            });

            AssertFixture(Fixtures.ClientIdentificationResult, new ClientIdentificationResult { Result = true });
        }

        [TestMethod, TestCategory("Protocol")]
        public void ModernFileTransferContractsEmitPinnedWireFixtures()
        {
            AssertFixture(Fixtures.FileTransferRequest, new FileTransferRequest { Id = 7, RemotePath = @"C:\Temp\report.pdf" });

            AssertFixture(Fixtures.FileTransferChunk, new FileTransferChunk
            {
                Id = 7,
                FilePath = "report.pdf",
                FileSize = 3,
                Chunk = new FileChunk { Offset = 0, Data = new byte[] { 1, 2, 3 } }
            });
        }

        [TestMethod, TestCategory("Protocol")]
        public void ModernFileSystemContractsEmitPinnedWireFixtures()
        {
            AssertFixture(Fixtures.GetDrivesResponse, new GetDrivesResponse
            {
                Drives = new[] { new Drive { DisplayName = "Local Disk (C:)", RootDirectory = @"C:\" } }
            });

            AssertFixture(Fixtures.GetDirectoryResponse, new GetDirectoryResponse
            {
                RemotePath = @"C:\Temp",
                Items = new[]
                {
                    new FileSystemEntry
                    {
                        EntryType = FileType.File,
                        Name = "report.pdf",
                        Size = 12345,
                        LastAccessTimeUtc = new DateTime(2026, 05, 31, 12, 30, 00, DateTimeKind.Utc),
                        ContentType = ContentType.Pdf
                    }
                }
            });

            AssertFixture(Fixtures.DoPathRename, new DoPathRename
            {
                Path = @"C:\Temp\old.txt",
                NewPath = @"C:\Temp\new.txt",
                PathType = FileType.File
            });

            AssertFixture(Fixtures.DoPathDelete, new DoPathDelete
            {
                Path = @"C:\Temp\old.txt",
                PathType = FileType.File
            });

            AssertFixture(Fixtures.SetStatusFileManager, new SetStatusFileManager
            {
                Message = "Directory loaded",
                SetLastDirectorySeen = true
            });
        }

        private static void AssertFixture<T>(string expectedHex, T value)
        {
            CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), Serialize(value));
            Assert.IsNotNull(Deserialize<T>(expectedHex));
        }

        private static byte[] Serialize<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                RuntimeTypeModel.Create().Serialize(stream, value);
                return stream.ToArray();
            }
        }

        private static T Deserialize<T>(string hex)
        {
            using (var stream = new MemoryStream(Convert.FromHexString(hex)))
            {
                return (T)RuntimeTypeModel.Create().Deserialize(stream, null, typeof(T));
            }
        }

        private static class Fixtures
        {
            public const string ClientIdentification = "0A05312E342E31121157696E646F7773203131203634204269741A0541646D696E220D556E69746564205374617465732A02555330E6013A404141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414141414142056A706C6F754A0E4C4F434154494F4E52454D4F544552066C65676163795A08746573742D6B6579620401020304";
            public const string ClientIdentificationResult = "0801";
            public const string FileTransferRequest = "08071212433A5C54656D705C7265706F72742E706466";
            public const string FileTransferChunk = "0807120A7265706F72742E706466180322051203010203";
            public const string GetDrivesResponse = "0A160A0F4C6F63616C204469736B2028433A291203433A5C";
            public const string GetDirectoryResponse = "0A07433A5C54656D70121A120A7265706F72742E70646618B960220708DCF1A51C10022807";
            public const string DoPathRename = "0A0F433A5C54656D705C6F6C642E747874120F433A5C54656D705C6E65772E747874";
            public const string DoPathDelete = "0A0F433A5C54656D705C6F6C642E747874";
            public const string SetStatusFileManager = "0A104469726563746F7279206C6F616465641001";
        }
    }
}
