using ProtoBuf;
using MasterSplinter.Common.Models;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class GetConnectionsResponse : IMessage
    {
        [ProtoMember(1)]
        public TcpConnection[] Connections { get; set; }
    }
}
