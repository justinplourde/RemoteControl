using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Messages.ReverseProxy;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.ReverseProxy
{
    public interface IReverseProxyProvider
    {
        Task ConnectAsync(
            ReverseProxyConnect message,
            Func<IMessage, CancellationToken, Task> sendAsync,
            CancellationToken cancellationToken);

        Task SendDataAsync(
            ReverseProxyData message,
            Func<IMessage, CancellationToken, Task> sendAsync,
            CancellationToken cancellationToken);

        Task DisconnectAsync(
            ReverseProxyDisconnect message,
            Func<IMessage, CancellationToken, Task> sendAsync,
            CancellationToken cancellationToken);
    }
}
