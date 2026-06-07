using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Shell
{
    public sealed class ShellCommandProvider : IShellCommandProvider, IDisposable
    {
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private readonly ConcurrentQueue<ShellOutput> _output = new ConcurrentQueue<ShellOutput>();
        private readonly SemaphoreSlim _outputSignal = new SemaphoreSlim(0);
        private Process _process;
        private StreamWriter _inputWriter;

        public async Task<ShellCommandResult> ExecuteAsync(string command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command))
                return ShellCommandResult.Error("Shell command is required.");

            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (string.Equals(command.Trim(), "exit", StringComparison.OrdinalIgnoreCase))
                {
                    DisposeSession();
                    return ShellCommandResult.Success("\n>> Session closed\n");
                }

                bool created = EnsureSession();
                ClearOutputQueue();

                string marker = "__MASTERSPLINTER_DONE_" + Guid.NewGuid().ToString("N") + "__";
                await _inputWriter.WriteLineAsync(command).ConfigureAwait(false);
                await _inputWriter.WriteLineAsync("echo " + marker).ConfigureAwait(false);
                await _inputWriter.FlushAsync().ConfigureAwait(false);

                ShellCommandOutput output = await ReadUntilMarkerAsync(marker, created, cancellationToken)
                    .ConfigureAwait(false);
                return output.IsError
                    ? ShellCommandResult.Error(output.Text)
                    : ShellCommandResult.Success(output.Text);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return ShellCommandResult.Error(exception.Message);
            }
            finally
            {
                _gate.Release();
            }
        }

        private bool EnsureSession()
        {
            if (_process != null && !_process.HasExited)
                return false;

            DisposeSession();
            _process = new Process
            {
                StartInfo = CreateStartInfo()
            };
            _process.Start();
            _inputWriter = _process.StandardInput;
            _ = Task.Run(() => ReadStreamAsync(_process.StandardOutput, false));
            _ = Task.Run(() => ReadStreamAsync(_process.StandardError, true));
            return true;
        }

        private static ProcessStartInfo CreateStartInfo()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (OperatingSystem.IsWindows())
            {
                startInfo.FileName = Environment.GetEnvironmentVariable("COMSPEC") ?? "cmd.exe";
                startInfo.ArgumentList.Add("/Q");
                startInfo.ArgumentList.Add("/K");
            }
            else
            {
                startInfo.FileName = "/bin/sh";
            }

            return startInfo;
        }

        private async Task<ShellCommandOutput> ReadUntilMarkerAsync(string marker, bool created, CancellationToken cancellationToken)
        {
            var builder = new StringBuilder();
            bool isError = false;
            if (created)
                builder.Append("\n>> New Session created\n");

            while (true)
            {
                ShellOutput output = await WaitForOutputAsync(cancellationToken).ConfigureAwait(false);
                if (output.Text.IndexOf(marker, StringComparison.Ordinal) >= 0)
                    break;

                isError |= output.IsError;
                builder.Append(output.Text);
            }

            isError |= await DrainBufferedOutputAsync(builder, cancellationToken).ConfigureAwait(false);
            return new ShellCommandOutput(builder.ToString(), isError);
        }

        private async Task<bool> DrainBufferedOutputAsync(StringBuilder builder, CancellationToken cancellationToken)
        {
            bool isError = false;
            using (var drain = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                drain.CancelAfter(TimeSpan.FromMilliseconds(100));
                while (!drain.IsCancellationRequested)
                {
                    ShellOutput output;
                    try
                    {
                        output = await WaitForOutputAsync(drain.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (drain.IsCancellationRequested)
                    {
                        return isError;
                    }

                    isError |= output.IsError;
                    builder.Append(output.Text);
                }
            }

            return isError;
        }

        private async Task<ShellOutput> WaitForOutputAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (_output.TryDequeue(out ShellOutput output))
                    return output;

                await _outputSignal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ReadStreamAsync(StreamReader reader, bool isError)
        {
            try
            {
                while (true)
                {
                    string line = await reader.ReadLineAsync().ConfigureAwait(false);
                    if (line == null)
                        return;

                    _output.Enqueue(new ShellOutput(line + Environment.NewLine, isError));
                    _outputSignal.Release();
                }
            }
            catch
            {
            }
        }

        private void ClearOutputQueue()
        {
            while (_output.TryDequeue(out _))
            {
            }
        }

        public void Dispose()
        {
            DisposeSession();
            _gate.Dispose();
            _outputSignal.Dispose();
        }

        private void DisposeSession()
        {
            try
            {
                _inputWriter?.Dispose();
            }
            catch
            {
            }

            _inputWriter = null;
            if (_process == null)
                return;

            try
            {
                if (!_process.HasExited)
                    _process.Kill();
            }
            catch
            {
            }

            _process.Dispose();
            _process = null;
        }

        private sealed class ShellOutput
        {
            public ShellOutput(string text, bool isError)
            {
                Text = text;
                IsError = isError;
            }

            public string Text { get; }

            public bool IsError { get; }
        }

        private sealed class ShellCommandOutput
        {
            public ShellCommandOutput(string text, bool isError)
            {
                Text = text;
                IsError = isError;
            }

            public string Text { get; }

            public bool IsError { get; }
        }
    }
}
