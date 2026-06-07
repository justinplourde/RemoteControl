using System;
using System.Security.Principal;

namespace MasterSplinter.Client.Core.Identity
{
    public sealed class ClientPrivilegeProvider : IClientPrivilegeProvider
    {
        private readonly Func<bool> _isAdministrator;

        public ClientPrivilegeProvider()
            : this(IsCurrentProcessAdministrator)
        {
        }

        public ClientPrivilegeProvider(Func<bool> isAdministrator)
        {
            _isAdministrator = isAdministrator ?? throw new ArgumentNullException(nameof(isAdministrator));
        }

        public string GetAccountType()
        {
            return _isAdministrator() ? "Admin" : "User";
        }

        private static bool IsCurrentProcessAdministrator()
        {
            if (!OperatingSystem.IsWindows())
                return false;

            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    var principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
