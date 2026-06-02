namespace MasterSplinter.Client.Core.Registry
{
    public interface IRegistryKeyProvider
    {
        RegistryKeyLoadResult LoadKey(string rootKeyName);
    }
}
