using MasterSplinter.Common.Enums;

namespace MasterSplinter.Server.Core.Sessions
{
    public sealed class ClientStatusSnapshot
    {
        public ClientStatusSnapshot(string statusMessage, UserStatus? userStatus)
        {
            StatusMessage = statusMessage;
            UserStatus = userStatus;
        }

        public string StatusMessage { get; }

        public UserStatus? UserStatus { get; }
    }
}
