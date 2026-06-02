using System;
using System.Diagnostics;

namespace MasterSplinter.Client.Core.Processes
{
    public sealed class ProcessStartProvider : IProcessStartProvider
    {
        public ProcessStartResult StartProcess(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return ProcessStartResult.Error();

            try
            {
                using (Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                }))
                {
                }

                return ProcessStartResult.Success();
            }
            catch (Exception)
            {
                return ProcessStartResult.Error();
            }
        }
    }
}
