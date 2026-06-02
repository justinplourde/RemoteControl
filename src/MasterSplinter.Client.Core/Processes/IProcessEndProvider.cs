namespace MasterSplinter.Client.Core.Processes
{
    public interface IProcessEndProvider
    {
        ProcessEndResult EndProcess(int pid);
    }
}
