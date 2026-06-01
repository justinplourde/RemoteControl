using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Auditing
{
    public interface IServerAuditSink
    {
        Task WriteAsync(ServerAuditEvent auditEvent, CancellationToken cancellationToken);
    }
}
