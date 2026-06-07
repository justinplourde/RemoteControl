using ProtoBuf;
using MasterSplinter.Common.Enums;

namespace MasterSplinter.Common.Models
{
    [ProtoContract]
    public class StartupItem
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Path { get; set; }

        [ProtoMember(3)]
        public StartupType Type { get; set; }
    }
}
