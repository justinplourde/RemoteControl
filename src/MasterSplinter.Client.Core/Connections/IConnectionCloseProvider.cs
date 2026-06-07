namespace MasterSplinter.Client.Core.Connections
{
    public interface IConnectionCloseProvider
    {
        TcpConnectionCloseResult CloseConnection(string localAddress, ushort localPort, string remoteAddress, ushort remotePort);
    }
}
