namespace MasterSplinter.Server.Core.Commands
{
    public interface ICommandDispatchPolicy
    {
        CommandDispatchPolicyDecision Authorize(
            CommandDispatchRequest request,
            CommandSafetyMetadata safetyMetadata);
    }
}
