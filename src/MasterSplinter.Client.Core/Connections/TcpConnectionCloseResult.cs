using MasterSplinter.Common.Models;

namespace MasterSplinter.Client.Core.Connections
{
    public sealed class TcpConnectionCloseResult
    {
        public TcpConnectionCloseResult(bool isSuccess, TcpConnection[] connections)
        {
            IsSuccess = isSuccess;
            Connections = connections ?? new TcpConnection[0];
        }

        public bool IsSuccess { get; }

        public TcpConnection[] Connections { get; }
    }
}
