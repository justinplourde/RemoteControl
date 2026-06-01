using Quasar.Common.Models;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class DriveListResult
    {
        private DriveListResult(Drive[] drives, string errorMessage)
        {
            Drives = drives;
            ErrorMessage = errorMessage;
        }

        public Drive[] Drives { get; }

        public string ErrorMessage { get; }

        public bool IsSuccess => ErrorMessage == null;

        public static DriveListResult Success(Drive[] drives)
        {
            return new DriveListResult(drives, null);
        }

        public static DriveListResult Error(string errorMessage)
        {
            return new DriveListResult(null, errorMessage);
        }
    }
}
