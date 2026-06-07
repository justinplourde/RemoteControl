using MasterSplinter.Client.Core.Identity;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MasterSplinter.Client.Core.Tests.Identity
{
    [TestClass]
    public class ClientPrivilegeProviderTests
    {
        [TestMethod, TestCategory("Identity")]
        public void ProviderReturnsAdminWhenProcessIsAdministrator()
        {
            var provider = new ClientPrivilegeProvider(() => true);

            Assert.AreEqual("Admin", provider.GetAccountType());
        }

        [TestMethod, TestCategory("Identity")]
        public void ProviderReturnsUserWhenProcessIsNotAdministrator()
        {
            var provider = new ClientPrivilegeProvider(() => false);

            Assert.AreEqual("User", provider.GetAccountType());
        }
    }
}
