using ProtoBuf;
using MasterSplinter.Common.Messages;
using System;
using System.IO;

namespace MasterSplinter.Common.Networking
{
    public class PayloadReader : MemoryStream
    {
        private readonly Stream _innerStream;
        public bool LeaveInnerStreamOpen { get; }

        public PayloadReader(byte[] payload, int length, bool leaveInnerStreamOpen)
        {
            _innerStream = new MemoryStream(payload, 0, length, false, true);
            LeaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        public PayloadReader(Stream stream, bool leaveInnerStreamOpen)
        {
            _innerStream = stream;
            LeaveInnerStreamOpen = leaveInnerStreamOpen;
        }

        public int ReadInteger()
        {
            return BitConverter.ToInt32(ReadBytes(4), 0);
        }

        public byte[] ReadBytes(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            if (_innerStream.CanSeek && _innerStream.Position + length > _innerStream.Length)
                throw new OverflowException($"Unable to read {length} bytes from stream");

            byte[] result = new byte[length];
            try
            {
                _innerStream.ReadExactly(result, 0, result.Length);
                return result;
            }
            catch (EndOfStreamException exception)
            {
                throw new OverflowException($"Unable to read {length} bytes from stream", exception);
            }
        }

        public IMessage ReadMessage()
        {
            TypeRegistry.EnsurePacketTypesRegistered();
            int length = ReadInteger();
            byte[] payload = ReadBytes(length);
            using (var stream = new MemoryStream(payload))
            {
                return Serializer.Deserialize<IMessage>(stream);
            }
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (LeaveInnerStreamOpen)
                {
                    _innerStream.Flush();
                }
                else
                {
                    _innerStream.Close();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }
    }
}
