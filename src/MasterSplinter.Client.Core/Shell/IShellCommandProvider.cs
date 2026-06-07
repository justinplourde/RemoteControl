using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Shell
{
    public interface IShellCommandProvider
    {
        Task<ShellCommandResult> ExecuteAsync(string command, CancellationToken cancellationToken);
    }
}
