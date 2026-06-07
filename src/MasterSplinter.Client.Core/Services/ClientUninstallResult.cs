using System;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class ClientUninstallResult
    {
        private ClientUninstallResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static ClientUninstallResult Success()
        {
            return new ClientUninstallResult(true, string.Empty);
        }

        public static ClientUninstallResult Error(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new ClientUninstallResult(false, errorMessage);
        }
    }
}
