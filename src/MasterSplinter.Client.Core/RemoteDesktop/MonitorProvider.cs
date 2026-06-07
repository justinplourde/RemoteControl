using System;
using System.Runtime.InteropServices;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public sealed class MonitorProvider : IMonitorProvider
    {
        private const int SM_CMONITORS = 80;

        public int GetMonitorCount()
        {
            if (!OperatingSystem.IsWindows())
                return 0;

            int count = GetSystemMetrics(SM_CMONITORS);
            return count < 0 ? 0 : count;
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);
    }
}
