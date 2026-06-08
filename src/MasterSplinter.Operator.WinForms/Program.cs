using System;
using System.Windows.Forms;

namespace MasterSplinter.Operator.WinForms
{
#pragma warning disable CA1416
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new RemoteDesktopForm());
        }
    }
#pragma warning restore CA1416
}
