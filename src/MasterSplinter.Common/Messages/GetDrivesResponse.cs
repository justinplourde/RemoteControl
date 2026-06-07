using ProtoBuf;
using MasterSplinter.Common.Models;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class GetDrivesResponse : IMessage
    {
        [ProtoMember(1)]
        public Drive[] Drives { get; set; }
    }
}
