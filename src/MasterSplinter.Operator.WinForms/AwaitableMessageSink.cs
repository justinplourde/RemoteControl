using MasterSplinter.Common.Messages;
using MasterSplinter.Server.Core.Listeners;
using MasterSplinter.Server.Core.RemoteDesktop;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Operator.WinForms
{
    internal sealed class AwaitableMessageSink : IRemoteClientMessageSink, IRemoteClientResponseSource
    {
        private readonly object _gate = new object();
        private readonly Dictionary<string, TaskCompletionSource<IMessage>> _pending =
            new Dictionary<string, TaskCompletionSource<IMessage>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Queue<IMessage>> _queued =
            new Dictionary<string, Queue<IMessage>>(StringComparer.OrdinalIgnoreCase);

        public async Task<IMessage> WaitForNextAsync(
            string clientId,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            return await WaitForNextAsync(clientId)
                .WaitAsync(timeout, cancellationToken)
                .ConfigureAwait(false);
        }

        public void CancelWait(string clientId)
        {
            TaskCompletionSource<IMessage> pending = null;
            lock (_gate)
            {
                if (_pending.TryGetValue(clientId, out pending))
                    _pending.Remove(clientId);
            }

            pending?.TrySetCanceled();
        }

        public Task HandleAsync(
            IRemoteClientConnection connection,
            IMessage message,
            CancellationToken cancellationToken)
        {
            string clientId = connection.ClientId ?? string.Empty;
            TaskCompletionSource<IMessage> pending = null;
            lock (_gate)
            {
                if (_pending.TryGetValue(clientId, out pending))
                {
                    _pending.Remove(clientId);
                }
                else
                {
                    GetQueue(clientId).Enqueue(message);
                }
            }

            pending?.TrySetResult(message);
            return Task.CompletedTask;
        }

        private Task<IMessage> WaitForNextAsync(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));

            lock (_gate)
            {
                Queue<IMessage> queue = GetQueue(clientId);
                if (queue.Count > 0)
                    return Task.FromResult(queue.Dequeue());

                if (_pending.ContainsKey(clientId))
                    throw new InvalidOperationException($"A dispatch is already waiting for client '{clientId}'.");

                var pending = new TaskCompletionSource<IMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pending.Add(clientId, pending);
                return pending.Task;
            }
        }

        private Queue<IMessage> GetQueue(string clientId)
        {
            if (!_queued.TryGetValue(clientId, out Queue<IMessage> queue))
            {
                queue = new Queue<IMessage>();
                _queued.Add(clientId, queue);
            }

            return queue;
        }
    }
}
