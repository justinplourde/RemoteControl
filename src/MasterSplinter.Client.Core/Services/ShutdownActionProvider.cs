using MasterSplinter.Common.Enums;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class ShutdownActionProvider : IShutdownActionProvider
    {
        private readonly Func<ProcessStartInfo, Process> _startProcess;
        private readonly Func<bool> _setSuspendState;

        public ShutdownActionProvider()
            : this(Process.Start, SuspendWindows)
        {
        }

        public ShutdownActionProvider(Func<ProcessStartInfo, Process> startProcess, Func<bool> setSuspendState)
        {
            _startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
            _setSuspendState = setSuspendState ?? throw new ArgumentNullException(nameof(setSuspendState));
        }

        public ShutdownActionResult RequestAction(ShutdownAction action)
        {
            if (!OperatingSystem.IsWindows())
                return ShutdownActionResult.Error("Shutdown actions are only supported on Windows.");

            try
            {
                switch (action)
                {
                    case ShutdownAction.Shutdown:
                        return StartShutdownProcess("/s /t 0");
                    case ShutdownAction.Restart:
                        return StartShutdownProcess("/r /t 0");
                    case ShutdownAction.Standby:
                        return _setSuspendState()
                            ? ShutdownActionResult.Success()
                            : ShutdownActionResult.Error(new Win32Exception(Marshal.GetLastWin32Error()).Message);
                    default:
                        return ShutdownActionResult.Error($"Unsupported shutdown action '{action}'.");
                }
            }
            catch (Exception exception)
            {
                return ShutdownActionResult.Error(exception.Message);
            }
        }

        private ShutdownActionResult StartShutdownProcess(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "shutdown",
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden,
                UseShellExecute = true
            };

            Process process = _startProcess(startInfo);
            return process == null
                ? ShutdownActionResult.Error("Process start returned no process.")
                : ShutdownActionResult.Success();
        }

        private static bool SuspendWindows()
        {
            return SetSuspendState(false, true, true);
        }

        [DllImport("PowrProf.dll", SetLastError = true)]
        private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    }
}
