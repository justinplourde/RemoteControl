using System;

namespace MasterSplinter.Client.Core.Startup
{
    public sealed class StartupItemMutationResult
    {
        private StartupItemMutationResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static StartupItemMutationResult Success()
        {
            return new StartupItemMutationResult(true, null);
        }

        public static StartupItemMutationResult Error(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new StartupItemMutationResult(false, errorMessage);
        }
    }
}
