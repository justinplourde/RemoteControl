using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Shell
{
    public sealed class ShellCommandProvider : IShellCommandProvider
    {
        public async Task<ShellCommandResult> ExecuteAsync(string command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command))
                return ShellCommandResult.Error("Shell command is required.");

            try
            {
                using (var process = new Process())
                {
                    process.StartInfo = CreateStartInfo(command);
                    process.Start();

                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                    string output = await outputTask.ConfigureAwait(false);
                    string error = await errorTask.ConfigureAwait(false);
                    string combined = CombineOutput(output, error);

                    return process.ExitCode == 0
                        ? ShellCommandResult.Success(combined)
                        : ShellCommandResult.Error(string.IsNullOrWhiteSpace(combined)
                            ? $"Shell command exited with code {process.ExitCode}."
                            : combined);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return ShellCommandResult.Error(exception.Message);
            }
        }

        private static ProcessStartInfo CreateStartInfo(string command)
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
                startInfo.ArgumentList.Add("/c");
            }
            else
            {
                startInfo.FileName = "/bin/sh";
                startInfo.ArgumentList.Add("-c");
            }

            startInfo.ArgumentList.Add(command);
            return startInfo;
        }

        private static string CombineOutput(string output, string error)
        {
            if (string.IsNullOrEmpty(output))
                return error ?? string.Empty;
            if (string.IsNullOrEmpty(error))
                return output;

            return output + error;
        }
    }
}
