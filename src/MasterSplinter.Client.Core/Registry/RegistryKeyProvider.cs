using Microsoft.Win32;
using MasterSplinter.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace MasterSplinter.Client.Core.Registry
{
    public sealed class RegistryKeyProvider : IRegistryKeyProvider
    {
        private const string DefaultValueName = "";

        public RegistryKeyLoadResult LoadKey(string rootKeyName)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryKeyLoadResult.Error("Registry access is only supported on Windows.");

            try
            {
                return RegistryKeyLoadResult.Success(LoadMatches(rootKeyName));
            }
            catch (Exception ex)
            {
                return RegistryKeyLoadResult.Error(ex.Message);
            }
        }

        [SupportedOSPlatform("windows")]
        private static RegSeekerMatch[] LoadMatches(string rootKeyName)
        {
            if (string.IsNullOrWhiteSpace(rootKeyName))
                return LoadRootMatches();

            string normalizedPath = NormalizeRootName(rootKeyName);
            using (RegistryKey root = OpenRootKey(normalizedPath))
            {
                if (root.Name != normalizedPath)
                {
                    string subKeyName = normalizedPath.Substring(root.Name.Length + 1);
                    using (RegistryKey subRoot = root.OpenSubKey(subKeyName, false))
                    {
                        if (subRoot == null)
                            return new RegSeekerMatch[0];

                        return LoadChildMatches(subRoot);
                    }
                }

                return LoadChildMatches(root);
            }
        }

        [SupportedOSPlatform("windows")]
        private static RegSeekerMatch[] LoadRootMatches()
        {
            var matches = new List<RegSeekerMatch>();
            foreach (RegistryHive hive in new[]
            {
                RegistryHive.ClassesRoot,
                RegistryHive.CurrentUser,
                RegistryHive.LocalMachine,
                RegistryHive.Users,
                RegistryHive.CurrentConfig
            })
            {
                using (RegistryKey key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64))
                {
                    matches.Add(CreateMatch(key, key.Name));
                }
            }

            return matches.ToArray();
        }

        [SupportedOSPlatform("windows")]
        private static RegSeekerMatch[] LoadChildMatches(RegistryKey rootKey)
        {
            var matches = new List<RegSeekerMatch>();
            foreach (string subKeyName in rootKey.GetSubKeyNames())
            {
                using (RegistryKey subKey = rootKey.OpenSubKey(subKeyName, false))
                {
                    matches.Add(subKey == null
                        ? CreateMissingMatch(subKeyName)
                        : CreateMatch(subKey, subKeyName));
                }
            }

            return matches.ToArray();
        }

        [SupportedOSPlatform("windows")]
        private static RegSeekerMatch CreateMatch(RegistryKey key, string keyName)
        {
            var values = new List<RegValueData>();
            foreach (string valueName in key.GetValueNames())
            {
                RegistryValueKind valueKind = key.GetValueKind(valueName);
                object value = key.GetValue(valueName);
                values.Add(CreateValueData(valueName, valueKind, value));
            }

            return new RegSeekerMatch
            {
                Key = keyName,
                Data = AddDefaultValue(values),
                HasSubKeys = key.SubKeyCount > 0
            };
        }

        [SupportedOSPlatform("windows")]
        private static RegSeekerMatch CreateMissingMatch(string keyName)
        {
            return new RegSeekerMatch
            {
                Key = keyName,
                Data = GetDefaultValues(),
                HasSubKeys = false
            };
        }

        [SupportedOSPlatform("windows")]
        private static RegValueData[] AddDefaultValue(List<RegValueData> values)
        {
            if (!values.Any(value => string.IsNullOrEmpty(value.Name)))
                values.Add(CreateValueData(DefaultValueName, RegistryValueKind.String, null));

            return values.ToArray();
        }

        [SupportedOSPlatform("windows")]
        private static RegValueData[] GetDefaultValues()
        {
            return new[] { CreateValueData(DefaultValueName, RegistryValueKind.String, null) };
        }

        [SupportedOSPlatform("windows")]
        private static RegValueData CreateValueData(string name, RegistryValueKind kind, object value)
        {
            return new RegValueData
            {
                Name = name,
                Kind = kind,
                Data = ConvertValueToBytes(kind, value)
            };
        }

        [SupportedOSPlatform("windows")]
        private static byte[] ConvertValueToBytes(RegistryValueKind kind, object value)
        {
            if (value == null)
                return new byte[0];

            switch (kind)
            {
                case RegistryValueKind.Binary:
                    return (byte[])value;
                case RegistryValueKind.MultiString:
                    return GetStringArrayBytes((string[])value);
                case RegistryValueKind.DWord:
                    return BitConverter.GetBytes((uint)(int)value);
                case RegistryValueKind.QWord:
                    return BitConverter.GetBytes((ulong)(long)value);
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return GetStringBytes((string)value);
                default:
                    return new byte[0];
            }
        }

        private static byte[] GetStringBytes(string value)
        {
            if (value == null)
                return new byte[0];

            byte[] bytes = new byte[value.Length * sizeof(char)];
            Buffer.BlockCopy(value.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static byte[] GetStringArrayBytes(string[] values)
        {
            if (values == null)
                return new byte[0];

            var bytes = new List<byte>();
            foreach (string value in values)
            {
                bytes.AddRange(GetStringBytes(value));
                bytes.Add(0);
                bytes.Add(0);
            }

            return bytes.ToArray();
        }

        [SupportedOSPlatform("windows")]
        private static RegistryKey OpenRootKey(string path)
        {
            string rootName = path.Split('\\')[0];
            switch (rootName)
            {
                case "HKEY_CLASSES_ROOT":
                    return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Registry64);
                case "HKEY_CURRENT_USER":
                    return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
                case "HKEY_LOCAL_MACHINE":
                    return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                case "HKEY_USERS":
                    return RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
                case "HKEY_CURRENT_CONFIG":
                    return RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Registry64);
                default:
                    throw new InvalidOperationException("Invalid rootkey, could not be found.");
            }
        }

        private static string NormalizeRootName(string path)
        {
            string[] parts = path.Split(new[] { '\\' }, 2);
            string root = parts[0];
            string normalizedRoot;
            switch (root.ToUpperInvariant())
            {
                case "HKCR":
                    normalizedRoot = "HKEY_CLASSES_ROOT";
                    break;
                case "HKCU":
                    normalizedRoot = "HKEY_CURRENT_USER";
                    break;
                case "HKLM":
                    normalizedRoot = "HKEY_LOCAL_MACHINE";
                    break;
                case "HKU":
                    normalizedRoot = "HKEY_USERS";
                    break;
                case "HKCC":
                    normalizedRoot = "HKEY_CURRENT_CONFIG";
                    break;
                default:
                    normalizedRoot = root;
                    break;
            }

            return parts.Length == 1 ? normalizedRoot : normalizedRoot + "\\" + parts[1];
        }
    }
}
