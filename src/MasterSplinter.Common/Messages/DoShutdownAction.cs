using ProtoBuf;
using MasterSplinter.Common.Enums;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class DoShutdownAction : IMessage
    {
        [ProtoMember(1)]
        public ShutdownAction Action { get; set; }
    }
}
