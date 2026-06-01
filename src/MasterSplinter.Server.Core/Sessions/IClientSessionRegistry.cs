using System.Collections.Generic;

namespace MasterSplinter.Server.Core.Sessions
{
    public interface IClientSessionRegistry
    {
        int Count { get; }

        void AddOrUpdate(IRemoteClientSession session);

        bool TryGet(string clientId, out IRemoteClientSession session);

        bool Remove(string clientId);

        IReadOnlyList<ClientSessionSnapshot> GetSnapshots();
    }
}
