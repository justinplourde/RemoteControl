using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProtoBuf;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Models;
using System;
using System.IO;

namespace MasterSplinter.Common.Tests.Models
{
    [TestClass]
    public class ModelSerializationTests
    {
        [TestMethod, TestCategory("Models")]
        public void FileChunkRoundTripsThroughProtobuf()
        {
            var original = new FileChunk
            {
                Offset = 4096,
                Data = new byte[] { 10, 20, 30 }
            };

            var roundTripped = RoundTrip(original);

            Assert.AreEqual(original.Offset, roundTripped.Offset);
            CollectionAssert.AreEqual(original.Data, roundTripped.Data);
        }

        [TestMethod, TestCategory("Models")]
        public void FileSystemEntryRoundTripsThroughProtobuf()
        {
            var timestamp = new DateTime(2026, 05, 31, 12, 30, 00, DateTimeKind.Utc);
            var original = new FileSystemEntry
            {
                EntryType = FileType.File,
                Name = "report.pdf",
                Size = 12345,
                LastAccessTimeUtc = timestamp,
                ContentType = ContentType.Pdf
            };

            var roundTripped = RoundTrip(original);

            Assert.AreEqual(original.EntryType, roundTripped.EntryType);
            Assert.AreEqual(original.Name, roundTripped.Name);
            Assert.AreEqual(original.Size, roundTripped.Size);
            Assert.AreEqual(original.LastAccessTimeUtc, roundTripped.LastAccessTimeUtc);
            Assert.AreEqual(original.ContentType, roundTripped.ContentType);
        }

        [TestMethod, TestCategory("Models")]
        public void DriveRoundTripsThroughProtobuf()
        {
            var original = new Drive
            {
                DisplayName = "Local Disk (C:)",
                RootDirectory = "C:\\"
            };

            var roundTripped = RoundTrip(original);

            Assert.AreEqual(original.DisplayName, roundTripped.DisplayName);
            Assert.AreEqual(original.RootDirectory, roundTripped.RootDirectory);
        }

        private static T RoundTrip<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, value);
                stream.Position = 0;
                return Serializer.Deserialize<T>(stream);
            }
        }
    }
}
