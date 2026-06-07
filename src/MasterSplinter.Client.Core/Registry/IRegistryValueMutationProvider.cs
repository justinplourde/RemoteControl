using MasterSplinter.Common.Models;
using Microsoft.Win32;

namespace MasterSplinter.Client.Core.Registry
{
    public interface IRegistryValueMutationProvider
    {
        RegistryValueMutationResult CreateValue(string keyPath, RegistryValueKind kind);

        RegistryValueMutationResult DeleteValue(string keyPath, string valueName);

        RegistryValueMutationResult RenameValue(string keyPath, string oldValueName, string newValueName);

        RegistryValueMutationResult ChangeValue(string keyPath, RegValueData value);
    }
}
