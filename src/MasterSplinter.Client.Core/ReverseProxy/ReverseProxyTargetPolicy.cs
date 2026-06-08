using System.Net;

namespace MasterSplinter.Client.Core.ReverseProxy
{
    public sealed class ReverseProxyTargetPolicy : IReverseProxyTargetPolicy
    {
        private ReverseProxyTargetPolicy()
        {
        }

        public static ReverseProxyTargetPolicy LoopbackOnly()
        {
            return new ReverseProxyTargetPolicy();
        }

        public bool IsAllowed(string target, int port)
        {
            if (string.IsNullOrWhiteSpace(target) || port < 1 || port > 65535)
                return false;

            if (IPAddress.TryParse(target, out IPAddress address))
                return IPAddress.IsLoopback(address);

            return string.Equals(target, "localhost", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
