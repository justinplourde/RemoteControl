using MasterSplinter.Common.Models;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class RegistryKeyLoadResult
    {
        private RegistryKeyLoadResult(RegSeekerMatch[] matches, bool isError, string errorMessage)
        {
            Matches = matches ?? new RegSeekerMatch[0];
            IsError = isError;
            ErrorMessage = errorMessage;
        }

        public RegSeekerMatch[] Matches { get; }

        public bool IsError { get; }

        public string ErrorMessage { get; }

        public static RegistryKeyLoadResult Success(RegSeekerMatch[] matches)
        {
            return new RegistryKeyLoadResult(matches, false, null);
        }

        public static RegistryKeyLoadResult Error(string errorMessage)
        {
            return new RegistryKeyLoadResult(new RegSeekerMatch[0], true, errorMessage);
        }
    }
}
