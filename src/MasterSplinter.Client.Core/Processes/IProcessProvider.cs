using RemoteProcess = MasterSplinter.Common.Models.Process;

namespace MasterSplinter.Client.Core.Processes
{
    public interface IProcessProvider
    {
        RemoteProcess[] GetProcesses();
    }
}
