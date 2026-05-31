using Quasar.Common.Utilities;
using System.Text;
using System.Text.RegularExpressions;

namespace Quasar.Common.Helpers
{
    public static class StringHelper
    {
        private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly string[] Sizes = { "B", "KB", "MB", "GB", "TB", "PB" };
        private static readonly SafeRandom Random = new SafeRandom();

        public static string GetRandomString(int length)
        {
            StringBuilder randomName = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                randomName.Append(Alphabet[Random.Next(Alphabet.Length)]);

            return randomName.ToString();
        }

        public static string GetHumanReadableFileSize(long size)
        {
            double len = size;
            int order = 0;
            while (len >= 1024 && order + 1 < Sizes.Length)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {Sizes[order]}";
        }

        public static string GetFormattedMacAddress(string macAddress)
        {
            return (macAddress.Length != 12)
                ? "00:00:00:00:00:00"
                : Regex.Replace(macAddress, "(.{2})(.{2})(.{2})(.{2})(.{2})(.{2})", "$1:$2:$3:$4:$5:$6");
        }

        public static string RemoveLastChars(string input, int amount = 2)
        {
            if (input.Length > amount)
                input = input.Remove(input.Length - amount);
            return input;
        }
    }
}
