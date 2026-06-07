using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class ClientUninstallProvider : IClientUninstallProvider
    {
        private readonly Func<string> _processPathProvider;

        public ClientUninstallProvider()
            : this(() => Environment.ProcessPath)
        {
        }

        public ClientUninstallProvider(Func<string> processPathProvider)
        {
            _processPathProvider = processPathProvider ?? throw new ArgumentNullException(nameof(processPathProvider));
        }

        public ClientUninstallResult Uninstall()
        {
            if (!OperatingSystem.IsWindows())
                return ClientUninstallResult.Error("Uninstall is only supported on Windows.");

            try
            {
                string processPath = _processPathProvider();
                if (string.IsNullOrWhiteSpace(processPath) || !File.Exists(processPath))
                    return ClientUninstallResult.Error("Uninstall failed: client executable path was not found.");

                if (string.Equals(Path.GetFileName(processPath), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
                    return ClientUninstallResult.Error("Uninstall requires a published client executable, not dotnet run.");

                string batchPath = CreateUninstallBatch(processPath);
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });

                return ClientUninstallResult.Success();
            }
            catch (Exception exception)
            {
                return ClientUninstallResult.Error(exception.Message);
            }
        }

        private static string CreateUninstallBatch(string processPath)
        {
            string batchPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cmd");
            string contents =
                "@echo off\r\n" +
                "chcp 65001 > nul\r\n" +
                "ping -n 10 localhost > nul\r\n" +
                "del /a /q /f " + Quote(processPath) + "\r\n" +
                "del /a /q /f " + Quote(batchPath) + "\r\n";

            File.WriteAllText(batchPath, contents, new UTF8Encoding(false));
            return batchPath;
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
