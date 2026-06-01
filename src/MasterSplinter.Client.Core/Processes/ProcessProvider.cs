using System.Diagnostics;
using RemoteProcess = Quasar.Common.Models.Process;

namespace MasterSplinter.Client.Core.Processes
{
    public sealed class ProcessProvider : IProcessProvider
    {
        public RemoteProcess[] GetProcesses()
        {
            Process[] processList = Process.GetProcesses();
            var processes = new RemoteProcess[processList.Length];

            for (int index = 0; index < processList.Length; index++)
            {
                Process process = processList[index];
                processes[index] = new RemoteProcess
                {
                    Name = process.ProcessName + ".exe",
                    Id = process.Id,
                    MainWindowTitle = process.MainWindowTitle
                };
            }

            return processes;
        }
    }
}
