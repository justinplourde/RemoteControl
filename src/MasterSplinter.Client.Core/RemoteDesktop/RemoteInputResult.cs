using System;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public sealed class RemoteInputResult
    {
        private RemoteInputResult(bool isSuccess, string errorMessage)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static RemoteInputResult Success()
        {
            return new RemoteInputResult(true, string.Empty);
        }

        public static RemoteInputResult Error(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new RemoteInputResult(false, errorMessage);
        }
    }
}
