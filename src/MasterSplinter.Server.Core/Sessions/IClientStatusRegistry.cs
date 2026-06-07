using MasterSplinter.Common.Enums;

namespace MasterSplinter.Server.Core.Sessions
{
    public interface IClientStatusRegistry
    {
        void SetStatus(string clientId, string statusMessage);

        void SetUserStatus(string clientId, UserStatus userStatus);

        bool TryGet(string clientId, out ClientStatusSnapshot snapshot);
    }
}
