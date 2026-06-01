using Quasar.Common.Models;

namespace MasterSplinter.Client.Core.Connections
{
    public interface IConnectionProvider
    {
        TcpConnection[] GetConnections();
    }
}
