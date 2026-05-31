using ProtoBuf;

namespace Quasar.Common.Protocol
{
    [ProtoContract]
    public class ProtocolVersion
    {
        [ProtoMember(1)]
        public int Major { get; set; }

        [ProtoMember(2)]
        public int Minor { get; set; }
    }
}
