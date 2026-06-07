using MasterSplinter.Common.Models;
using System;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class RegistryValueMutationResult
    {
        private RegistryValueMutationResult(bool isSuccess, string errorMessage, string valueName, RegValueData value)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            ValueName = valueName;
            Value = value;
        }

        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public string ValueName { get; }

        public RegValueData Value { get; }

        public static RegistryValueMutationResult Success(string valueName, RegValueData value)
        {
            return new RegistryValueMutationResult(true, string.Empty, valueName, value);
        }

        public static RegistryValueMutationResult Error(string errorMessage, string valueName = null, RegValueData value = null)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new RegistryValueMutationResult(false, errorMessage, valueName, value);
        }
    }
}
