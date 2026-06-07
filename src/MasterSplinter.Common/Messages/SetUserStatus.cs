using ProtoBuf;
using MasterSplinter.Common.Enums;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class SetUserStatus : IMessage
    {
        [ProtoMember(1)]
        public UserStatus Message { get; set; }
    }
}
