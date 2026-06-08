namespace MasterSplinter.Client.Core.ReverseProxy
{
    public interface IReverseProxyTargetPolicy
    {
        bool IsAllowed(string target, int port);
    }
}
