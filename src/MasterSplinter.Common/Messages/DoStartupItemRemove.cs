using ProtoBuf;
using MasterSplinter.Common.Models;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class DoStartupItemRemove : IMessage
    {
        [ProtoMember(1)]
        public StartupItem StartupItem { get; set; }
    }
}
