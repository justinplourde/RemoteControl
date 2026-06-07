using MasterSplinter.Common.Enums;

namespace MasterSplinter.Client.Core.Services
{
    public interface IShutdownActionProvider
    {
        ShutdownActionResult RequestAction(ShutdownAction action);
    }
}
