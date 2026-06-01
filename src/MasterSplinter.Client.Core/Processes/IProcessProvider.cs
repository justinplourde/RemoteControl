using RemoteProcess = Quasar.Common.Models.Process;

namespace MasterSplinter.Client.Core.Processes
{
    public interface IProcessProvider
    {
        RemoteProcess[] GetProcesses();
    }
}
