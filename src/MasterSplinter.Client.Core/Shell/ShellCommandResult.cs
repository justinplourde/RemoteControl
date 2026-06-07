using System;

namespace MasterSplinter.Client.Core.Shell
{
    public sealed class ShellCommandResult
    {
        private ShellCommandResult(bool isSuccess, string output)
        {
            IsSuccess = isSuccess;
            Output = output;
        }

        public bool IsSuccess { get; }

        public string Output { get; }

        public static ShellCommandResult Success(string output)
        {
            return new ShellCommandResult(true, output ?? string.Empty);
        }

        public static ShellCommandResult Error(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Shell error output is required.", nameof(output));

            return new ShellCommandResult(false, output);
        }
    }
}
