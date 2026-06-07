using MasterSplinter.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public interface IRemoteClientResponseSource
    {
        Task<IMessage> WaitForNextAsync(
            string clientId,
            TimeSpan timeout,
            CancellationToken cancellationToken);

        void CancelWait(string clientId);
    }
}
