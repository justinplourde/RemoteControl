namespace MasterSplinter.Client.Core.Registry
{
    public interface IRegistryKeyMutationProvider
    {
        RegistryKeyMutationResult CreateKey(string parentPath);

        RegistryKeyMutationResult DeleteKey(string parentPath, string keyName);

        RegistryKeyMutationResult RenameKey(string parentPath, string oldKeyName, string newKeyName);
    }
}
