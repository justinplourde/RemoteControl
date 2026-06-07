using MasterSplinter.Common.Models;
using System;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class RegistryKeyMutationResult
    {
        private RegistryKeyMutationResult(bool isSuccess, string errorMessage, string keyName, RegSeekerMatch match)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            KeyName = keyName;
            Match = match;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public string KeyName { get; }

        public RegSeekerMatch Match { get; }

        public static RegistryKeyMutationResult Success(string keyName, RegSeekerMatch match)
        {
            return new RegistryKeyMutationResult(true, string.Empty, keyName, match);
        }

        public static RegistryKeyMutationResult Error(string errorMessage, string keyName = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new RegistryKeyMutationResult(false, errorMessage, keyName, null);
        }
    }
}
