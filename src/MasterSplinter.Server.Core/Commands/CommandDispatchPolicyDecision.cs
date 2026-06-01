using System;

namespace MasterSplinter.Server.Core.Commands
{
    public sealed class CommandDispatchPolicyDecision
    {
        private CommandDispatchPolicyDecision(bool isAllowed, CommandDispatchStatus deniedStatus, string reason)
        {
            IsAllowed = isAllowed;
            DeniedStatus = deniedStatus;
            Reason = reason;
        }

        public bool IsAllowed { get; }

        public CommandDispatchStatus DeniedStatus { get; }

        public string Reason { get; }

        public static CommandDispatchPolicyDecision Allow()
        {
            return new CommandDispatchPolicyDecision(true, CommandDispatchStatus.Sent, null);
        }

        public static CommandDispatchPolicyDecision Deny(CommandDispatchStatus status, string reason)
        {
            if (status != CommandDispatchStatus.PermissionDenied &&
                status != CommandDispatchStatus.ConsentRequired)
            {
                throw new ArgumentException("Policy denials must use a policy denial status.", nameof(status));
            }

            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Denial reason is required.", nameof(reason));

            return new CommandDispatchPolicyDecision(false, status, reason);
        }
    }
}
