namespace MasterSplinter.Client.Core.Processes
{
    public interface IProcessStartProvider
    {
        ProcessStartResult StartProcess(string filePath);
    }
}
