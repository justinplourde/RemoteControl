using ProtoBuf;
using System.Collections.Generic;

namespace Quasar.Common.Protocol
{
    [ProtoContract]
    public class ClientCapabilities
    {
        [ProtoMember(1)]
        public List<string> SupportedFeatures { get; set; } = new List<string>();
    }
}
