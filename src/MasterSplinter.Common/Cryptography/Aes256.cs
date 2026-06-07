using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MasterSplinter.Common.Cryptography
{
    public class Aes256
    {
        private const int KeyLength = 32;
        private const int AuthKeyLength = 64;
        private const int IvLength = 16;
        private const int HmacSha256Length = 32;
        private readonly byte[] _key;
        private readonly byte[] _authKey;

        private static readonly byte[] Salt =
        {
            0xBF, 0xEB, 0x1E, 0x56, 0xFB, 0xCD, 0x97, 0x3B, 0xB2, 0x19, 0x02, 0x24, 0x30, 0xA5, 0x78, 0x43,
            0x00, 0x3D, 0x56, 0x44, 0xD2, 0x1E, 0x62, 0xB9, 0xD4, 0xF1, 0x80, 0xE7, 0xE6, 0xC3, 0x39, 0x41
        };

        public Aes256(string masterKey)
        {
            if (string.IsNullOrEmpty(masterKey))
                throw new ArgumentException($"{nameof(masterKey)} can not be null or empty.");

            byte[] derivedBytes = Rfc2898DeriveBytes.Pbkdf2(
                masterKey,
                Salt,
                50000,
                HashAlgorithmName.SHA1,
                KeyLength + AuthKeyLength);

            _key = new byte[KeyLength];
            _authKey = new byte[AuthKeyLength];
            Buffer.BlockCopy(derivedBytes, 0, _key, 0, KeyLength);
            Buffer.BlockCopy(derivedBytes, KeyLength, _authKey, 0, AuthKeyLength);
        }

        public string Encrypt(string input)
        {
            return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(input)));
        }

        public byte[] Encrypt(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException($"{nameof(input)} can not be null.");

            using (var ms = new MemoryStream())
            using (var aesProvider = Aes.Create())
            {
                ms.Position = HmacSha256Length;

                aesProvider.KeySize = 256;
                aesProvider.BlockSize = 128;
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Padding = PaddingMode.PKCS7;
                aesProvider.Key = _key;
                aesProvider.GenerateIV();

                ms.Write(aesProvider.IV, 0, aesProvider.IV.Length);

                using (var cs = new CryptoStream(ms, aesProvider.CreateEncryptor(), CryptoStreamMode.Write, true))
                {
                    cs.Write(input, 0, input.Length);
                    cs.FlushFinalBlock();
                }

                using (var hmac = new HMACSHA256(_authKey))
                {
                    byte[] encrypted = ms.ToArray();
                    byte[] hash = hmac.ComputeHash(encrypted, HmacSha256Length, encrypted.Length - HmacSha256Length);
                    ms.Position = 0;
                    ms.Write(hash, 0, hash.Length);
                }

                return ms.ToArray();
            }
        }

        public string Decrypt(string input)
        {
            return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(input)));
        }

        public byte[] Decrypt(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException($"{nameof(input)} can not be null.");

            using (var ms = new MemoryStream(input))
            using (var aesProvider = Aes.Create())
            {
                aesProvider.KeySize = 256;
                aesProvider.BlockSize = 128;
                aesProvider.Mode = CipherMode.CBC;
                aesProvider.Padding = PaddingMode.PKCS7;
                aesProvider.Key = _key;

                using (var hmac = new HMACSHA256(_authKey))
                {
                    var hash = hmac.ComputeHash(ms.ToArray(), HmacSha256Length, ms.ToArray().Length - HmacSha256Length);
                    byte[] receivedHash = new byte[HmacSha256Length];
                    ms.Read(receivedHash, 0, receivedHash.Length);

                    if (!SafeComparison.AreEqual(hash, receivedHash))
                        throw new CryptographicException("Invalid message authentication code (MAC).");
                }

                byte[] iv = new byte[IvLength];
                ms.Read(iv, 0, IvLength);
                aesProvider.IV = iv;

                using (var cs = new CryptoStream(ms, aesProvider.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    using (var plainText = new MemoryStream())
                    {
                        cs.CopyTo(plainText);
                        return plainText.ToArray();
                    }
                }
            }
        }
    }
}
