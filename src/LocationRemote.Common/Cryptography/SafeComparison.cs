using System.Security.Cryptography;

namespace Quasar.Common.Cryptography
{
    public class SafeComparison
    {
        public static bool AreEqual(byte[] a1, byte[] a2)
        {
            if (a1 == null || a2 == null || a1.Length != a2.Length)
                return false;

            return CryptographicOperations.FixedTimeEquals(a1, a2);
        }
    }
}
