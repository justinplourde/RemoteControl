using MasterSplinter.Common.Models;

namespace MasterSplinter.Client.Core.Connections
{
    public interface IConnectionProvider
    {
        TcpConnection[] GetConnections();
    }
}
