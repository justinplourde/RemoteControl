using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Sessions
{
    public interface IRemoteClientSession
    {
        string ClientId { get; }

        ClientIdentification Identification { get; }

        bool IsConnected { get; }

        DateTimeOffset ConnectedAtUtc { get; }

        DateTimeOffset LastSeenUtc { get; }

        Task SendAsync(IMessage message, CancellationToken cancellationToken);
    }
}
