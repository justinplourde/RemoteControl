using MasterSplinter.Client.Core.Identity;
using System;
using System.Diagnostics;
using System.Text;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class ElevationRequestProvider : IElevationRequestProvider
    {
        private readonly IClientPrivilegeProvider _privilegeProvider;
        private readonly Func<string> _processPath;
        private readonly Func<string[]> _commandLineArgs;
        private readonly Func<ProcessStartInfo, Process> _startProcess;

        public ElevationRequestProvider(IClientPrivilegeProvider privilegeProvider)
            : this(
                  privilegeProvider,
                  () => Environment.ProcessPath,
                  () => Environment.GetCommandLineArgs(),
                  Process.Start)
        {
        }

        public ElevationRequestProvider(
            IClientPrivilegeProvider privilegeProvider,
            Func<string> processPath,
            Func<string[]> commandLineArgs,
            Func<ProcessStartInfo, Process> startProcess)
        {
            _privilegeProvider = privilegeProvider ?? throw new ArgumentNullException(nameof(privilegeProvider));
            _processPath = processPath ?? throw new ArgumentNullException(nameof(processPath));
            _commandLineArgs = commandLineArgs ?? throw new ArgumentNullException(nameof(commandLineArgs));
            _startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
        }

        public ElevationRequestResult RequestElevation()
        {
            if (string.Equals(_privilegeProvider.GetAccountType(), "Admin", StringComparison.OrdinalIgnoreCase))
                return ElevationRequestResult.AlreadyElevated();

            if (!OperatingSystem.IsWindows())
                return ElevationRequestResult.Failed("Elevation request is only supported on Windows.");

            string fileName = _processPath();
            if (string.IsNullOrWhiteSpace(fileName))
                return ElevationRequestResult.Failed("Current executable path is not available.");

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = BuildArgumentString(_commandLineArgs()),
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process process = _startProcess(startInfo);
                return process == null ? ElevationRequestResult.Refused() : ElevationRequestResult.Requested();
            }
            catch
            {
                return ElevationRequestResult.Refused();
            }
        }

        private static string BuildArgumentString(string[] args)
        {
            if (args == null || args.Length <= 1)
                return string.Empty;

            var builder = new StringBuilder();
            for (int index = 1; index < args.Length; index++)
            {
                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(QuoteArgument(args[index]));
            }

            return builder.ToString();
        }

        private static string QuoteArgument(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";

            bool needsQuotes = value.IndexOfAny(new[] { ' ', '\t', '"' }) >= 0;
            string escaped = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
        }
    }
}
