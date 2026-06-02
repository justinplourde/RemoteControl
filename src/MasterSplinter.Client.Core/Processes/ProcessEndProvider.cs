using System;
using System.Diagnostics;

namespace MasterSplinter.Client.Core.Processes
{
    public sealed class ProcessEndProvider : IProcessEndProvider
    {
        public ProcessEndResult EndProcess(int pid)
        {
            if (pid <= 4 || pid == Process.GetCurrentProcess().Id)
                return ProcessEndResult.Error();

            try
            {
                using (Process process = Process.GetProcessById(pid))
                {
                    process.Kill();
                    return ProcessEndResult.Success();
                }
            }
            catch (Exception)
            {
                return ProcessEndResult.Error();
            }
        }
    }
}
