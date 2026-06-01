using System.Collections.Generic;
using Quasar.Common.Models;

namespace MasterSplinter.Client.Core.Startup
{
    public sealed class StartupItemsResult
    {
        private StartupItemsResult(List<StartupItem> startupItems, string errorMessage)
        {
            StartupItems = startupItems;
            ErrorMessage = errorMessage;
        }

        public List<StartupItem> StartupItems { get; }

        public string ErrorMessage { get; }

        public bool IsSuccess => ErrorMessage == null;

        public static StartupItemsResult Success(List<StartupItem> startupItems)
        {
            return new StartupItemsResult(startupItems, null);
        }

        public static StartupItemsResult Error(string errorMessage)
        {
            return new StartupItemsResult(null, errorMessage);
        }
    }
}
