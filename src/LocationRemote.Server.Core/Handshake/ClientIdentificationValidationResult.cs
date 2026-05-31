using System;

namespace LocationRemote.Server.Core.Handshake
{
    public sealed class ClientIdentificationValidationResult
    {
        private ClientIdentificationValidationResult(bool accepted, string rejectionReason)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
        }

        public bool Accepted { get; }

        public string RejectionReason { get; }

        public static ClientIdentificationValidationResult Accept()
        {
            return new ClientIdentificationValidationResult(true, null);
        }

        public static ClientIdentificationValidationResult Reject(string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
                throw new ArgumentException("Rejection reason is required.", nameof(rejectionReason));

            return new ClientIdentificationValidationResult(false, rejectionReason);
        }
    }
}
