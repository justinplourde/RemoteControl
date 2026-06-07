using System;

namespace MasterSplinter.Client.Core.Services
{
    public sealed class ElevationRequestResult
    {
        public ElevationRequestResult(ElevationRequestStatus status, string errorMessage)
        {
            Status = status;
            ErrorMessage = errorMessage;
        }

        public ElevationRequestStatus Status { get; }

        public string ErrorMessage { get; }

        public static ElevationRequestResult AlreadyElevated()
        {
            return new ElevationRequestResult(ElevationRequestStatus.AlreadyElevated, null);
        }

        public static ElevationRequestResult Requested()
        {
            return new ElevationRequestResult(ElevationRequestStatus.Requested, null);
        }

        public static ElevationRequestResult Refused()
        {
            return new ElevationRequestResult(ElevationRequestStatus.Refused, null);
        }

        public static ElevationRequestResult Failed(string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(errorMessage))
                throw new ArgumentException("Error message is required.", nameof(errorMessage));

            return new ElevationRequestResult(ElevationRequestStatus.Failed, errorMessage);
        }
    }
}
