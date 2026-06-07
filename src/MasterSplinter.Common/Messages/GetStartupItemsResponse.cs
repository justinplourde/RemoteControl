using ProtoBuf;
using MasterSplinter.Common.Models;
using System.Collections.Generic;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class GetStartupItemsResponse : IMessage
    {
        [ProtoMember(1)]
        public List<StartupItem> StartupItems { get; set; }
    }
}
