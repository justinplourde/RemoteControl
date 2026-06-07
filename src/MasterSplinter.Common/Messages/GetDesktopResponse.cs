using ProtoBuf;
using MasterSplinter.Common.Video;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class GetDesktopResponse : IMessage
    {
        [ProtoMember(1)]
        public byte[] Image { get; set; }

        [ProtoMember(2)]
        public int Quality { get; set; }

        [ProtoMember(3)]
        public int Monitor { get; set; }

        [ProtoMember(4)]
        public Resolution Resolution { get; set; }
    }
}
