using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Server.Core.Auditing
{
    public sealed class NoOpServerAuditSink : IServerAuditSink
    {
        public static readonly NoOpServerAuditSink Instance = new NoOpServerAuditSink();

        private NoOpServerAuditSink()
        {
        }

        public Task WriteAsync(ServerAuditEvent auditEvent, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
