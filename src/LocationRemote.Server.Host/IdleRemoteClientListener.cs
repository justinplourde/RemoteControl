using LocationRemote.Server.Core.Listeners;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Host
{
    internal sealed class IdleRemoteClientListener : IRemoteClientListener
    {
        public bool IsListening { get; private set; }

        public ServerListenOptions Options { get; private set; }

        public IRemoteClientListenerHandler Handler { get; private set; }

        public Task StartAsync(
            ServerListenOptions options,
            IRemoteClientListenerHandler handler,
            CancellationToken cancellationToken)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            IsListening = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            IsListening = false;
            return Task.CompletedTask;
        }
    }
}
