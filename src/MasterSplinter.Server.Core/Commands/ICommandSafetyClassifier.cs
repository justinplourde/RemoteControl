using MasterSplinter.Common.Messages;

namespace MasterSplinter.Server.Core.Commands
{
    public interface ICommandSafetyClassifier
    {
        CommandSafetyMetadata Classify(IMessage message);
    }
}
