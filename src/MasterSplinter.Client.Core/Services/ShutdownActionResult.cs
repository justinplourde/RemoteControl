using System;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class ShutdownActionResult
    {
        public ShutdownActionResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static ShutdownActionResult Success()
        {
            return new ShutdownActionResult(true, null);
        }

        public static ShutdownActionResult Error(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new ShutdownActionResult(false, errorMessage);
        }
    }
}
