using MasterSplinter.Common.Models;

namespace MasterSplinter.Client.Core.Startup
{
    public interface IStartupItemMutationProvider
    {
        StartupItemMutationResult AddStartupItem(StartupItem startupItem);

        StartupItemMutationResult RemoveStartupItem(StartupItem startupItem);
    }
}
