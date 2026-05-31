using System.Security.Cryptography;
using System.Text;

namespace Quasar.Common.Cryptography
{
    public static class Sha256
    {
        public static string ComputeHash(string input)
        {
            byte[] data = ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = new StringBuilder();

            foreach (byte value in data)
                hash.Append(value.ToString("X2"));

            return hash.ToString().ToUpper();
        }

        public static byte[] ComputeHash(byte[] input)
        {
            using (var sha = SHA256.Create())
            {
                return sha.ComputeHash(input);
            }
        }
    }
}
