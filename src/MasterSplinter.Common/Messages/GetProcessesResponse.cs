using ProtoBuf;
using MasterSplinter.Common.Models;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class GetProcessesResponse : IMessage
    {
        [ProtoMember(1)]
        public Process[] Processes { get; set; }
    }
}
