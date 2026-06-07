using ProtoBuf;
using MasterSplinter.Common.Models;
using System.Collections.Generic;

namespace MasterSplinter.Common.Messages
{
    [ProtoContract]
    public class GetPasswordsResponse : IMessage
    {
        [ProtoMember(1)]
        public List<RecoveredAccount> RecoveredAccounts { get; set; }
    }
}
