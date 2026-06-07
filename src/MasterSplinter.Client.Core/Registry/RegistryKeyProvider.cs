using Microsoft.Win32;
using MasterSplinter.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;

namespace MasterSplinter.Client.Core.Registry
{
#pragma warning disable CA1416
    public sealed class RegistryKeyProvider : IRegistryKeyProvider, IRegistryKeyMutationProvider, IRegistryValueMutationProvider
    {
        private const string DefaultValueName = "";
        private const string RegistryKeyCreateError = "Cannot create key: Error writing to the registry";
        private const string RegistryKeyDeleteError = "Cannot delete key: Error writing to the registry";
        private const string RegistryKeyRenameError = "Cannot rename key: Error writing to the registry";
        private const string RegistryValueCreateError = "Cannot create value: Error writing to the registry";
        private const string RegistryValueDeleteError = "Cannot delete value: Error writing to the registry";
        private const string RegistryValueRenameError = "Cannot rename value: Error writing to the registry";
        private const string RegistryValueChangeError = "Cannot change value: Error writing to the registry";

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

        public RegistryKeyMutationResult CreateKey(string parentPath)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryKeyMutationResult.Error("Registry access is only supported on Windows.");

            string name = string.Empty;
            try
            {
                using (RegistryKey parent = OpenWritableKey(parentPath))
                {
                    if (parent == null)
                        return RegistryKeyMutationResult.Error(GetWriteAccessError(parentPath), name);

                    int index = 1;
                    name = $"New Key #{index}";
                    while (ContainsSubKey(parent, name))
                    {
                        index++;
                        name = $"New Key #{index}";
                    }

                    using (RegistryKey child = parent.CreateSubKey(name, true))
                    {
                        if (child == null)
                            return RegistryKeyMutationResult.Error(RegistryKeyCreateError, name);
                    }
                }

                return RegistryKeyMutationResult.Success(name, new RegSeekerMatch
                {
                    Key = name,
                    Data = GetDefaultValues(),
                    HasSubKeys = false
                });
            }
            catch (Exception exception)
            {
                return RegistryKeyMutationResult.Error(exception.Message, name);
            }
        }

        public RegistryKeyMutationResult DeleteKey(string parentPath, string keyName)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryKeyMutationResult.Error("Registry access is only supported on Windows.", keyName);

            try
            {
                ValidateChildKeyName(keyName);
                using (RegistryKey parent = OpenWritableKey(parentPath))
                {
                    if (parent == null)
                        return RegistryKeyMutationResult.Error(GetWriteAccessError(parentPath), keyName);

                    if (!ContainsSubKey(parent, keyName))
                        return RegistryKeyMutationResult.Success(keyName, null);

                    try
                    {
                        parent.DeleteSubKeyTree(keyName);
                    }
                    catch
                    {
                        return RegistryKeyMutationResult.Error(RegistryKeyDeleteError, keyName);
                    }
                }

                return RegistryKeyMutationResult.Success(keyName, null);
            }
            catch (Exception exception)
            {
                return RegistryKeyMutationResult.Error(exception.Message, keyName);
            }
        }

        public RegistryKeyMutationResult RenameKey(string parentPath, string oldKeyName, string newKeyName)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryKeyMutationResult.Error("Registry access is only supported on Windows.", oldKeyName);

            try
            {
                ValidateChildKeyName(oldKeyName);
                ValidateChildKeyName(newKeyName);
                using (RegistryKey parent = OpenWritableKey(parentPath))
                {
                    if (parent == null)
                        return RegistryKeyMutationResult.Error(GetWriteAccessError(parentPath), oldKeyName);
                    if (!ContainsSubKey(parent, oldKeyName))
                        return RegistryKeyMutationResult.Error($"The registry: {oldKeyName} does not exist in: {parentPath}", oldKeyName);
                    if (ContainsSubKey(parent, newKeyName))
                        return RegistryKeyMutationResult.Error(RegistryKeyRenameError, oldKeyName);

                    try
                    {
                        CopySubKey(parent, oldKeyName, newKeyName);
                        parent.DeleteSubKeyTree(oldKeyName);
                    }
                    catch
                    {
                        return RegistryKeyMutationResult.Error(RegistryKeyRenameError, oldKeyName);
                    }
                }

                return RegistryKeyMutationResult.Success(newKeyName, null);
            }
            catch (Exception exception)
            {
                return RegistryKeyMutationResult.Error(exception.Message, oldKeyName);
            }
        }

        public RegistryValueMutationResult CreateValue(string keyPath, RegistryValueKind kind)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryValueMutationResult.Error("Registry access is only supported on Windows.");

            string name = string.Empty;
            try
            {
                using (RegistryKey key = OpenWritableKey(keyPath))
                {
                    if (key == null)
                        return RegistryValueMutationResult.Error(GetWriteAccessError(keyPath), name);

                    int index = 1;
                    name = $"New Value #{index}";
                    while (ContainsValue(key, name))
                    {
                        index++;
                        name = $"New Value #{index}";
                    }

                    object defaultValue = GetDefaultValue(kind);
                    try
                    {
                        key.SetValue(name, defaultValue, kind);
                    }
                    catch
                    {
                        return RegistryValueMutationResult.Error(RegistryValueCreateError, name);
                    }

                    return RegistryValueMutationResult.Success(name, CreateValueData(name, kind, defaultValue));
                }
            }
            catch (Exception exception)
            {
                return RegistryValueMutationResult.Error(exception.Message, name);
            }
        }

        public RegistryValueMutationResult DeleteValue(string keyPath, string valueName)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryValueMutationResult.Error("Registry access is only supported on Windows.", valueName);

            try
            {
                ValidateValueName(valueName);
                using (RegistryKey key = OpenWritableKey(keyPath))
                {
                    if (key == null)
                        return RegistryValueMutationResult.Error(GetWriteAccessError(keyPath), valueName);

                    if (!ContainsValue(key, valueName))
                        return RegistryValueMutationResult.Success(valueName, null);

                    try
                    {
                        key.DeleteValue(valueName);
                    }
                    catch
                    {
                        return RegistryValueMutationResult.Error(RegistryValueDeleteError, valueName);
                    }
                }

                return RegistryValueMutationResult.Success(valueName, null);
            }
            catch (Exception exception)
            {
                return RegistryValueMutationResult.Error(exception.Message, valueName);
            }
        }

        public RegistryValueMutationResult RenameValue(string keyPath, string oldValueName, string newValueName)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryValueMutationResult.Error("Registry access is only supported on Windows.", oldValueName);

            try
            {
                ValidateValueName(oldValueName);
                ValidateValueName(newValueName);
                using (RegistryKey key = OpenWritableKey(keyPath))
                {
                    if (key == null)
                        return RegistryValueMutationResult.Error(GetWriteAccessError(keyPath), oldValueName);
                    if (!ContainsValue(key, oldValueName))
                        return RegistryValueMutationResult.Error($"The value: {oldValueName} does not exist in: {keyPath}", oldValueName);
                    if (ContainsValue(key, newValueName))
                        return RegistryValueMutationResult.Error(RegistryValueRenameError, oldValueName);

                    try
                    {
                        object value = key.GetValue(oldValueName);
                        RegistryValueKind kind = key.GetValueKind(oldValueName);
                        key.SetValue(newValueName, value, kind);
                        key.DeleteValue(oldValueName);
                    }
                    catch
                    {
                        try
                        {
                            key.DeleteValue(newValueName, false);
                        }
                        catch
                        {
                        }

                        return RegistryValueMutationResult.Error(RegistryValueRenameError, oldValueName);
                    }
                }

                return RegistryValueMutationResult.Success(newValueName, null);
            }
            catch (Exception exception)
            {
                return RegistryValueMutationResult.Error(exception.Message, oldValueName);
            }
        }

        public RegistryValueMutationResult ChangeValue(string keyPath, RegValueData value)
        {
            if (!OperatingSystem.IsWindows())
                return RegistryValueMutationResult.Error("Registry access is only supported on Windows.", value == null ? null : value.Name, value);
            if (value == null)
                return RegistryValueMutationResult.Error("Registry value is required.");

            try
            {
                ValidateValueName(value.Name);
                using (RegistryKey key = OpenWritableKey(keyPath))
                {
                    if (key == null)
                        return RegistryValueMutationResult.Error(GetWriteAccessError(keyPath), value.Name, value);
                    if (!IsDefaultValue(value.Name) && !ContainsValue(key, value.Name))
                        return RegistryValueMutationResult.Error($"The value: {value.Name} does not exist in: {keyPath}", value.Name, value);

                    try
                    {
                        key.SetValue(value.Name, ConvertBytesToValue(value.Kind, value.Data), value.Kind);
                    }
                    catch
                    {
                        return RegistryValueMutationResult.Error(RegistryValueChangeError, value.Name, value);
                    }
                }

                return RegistryValueMutationResult.Success(value.Name, value);
            }
            catch (Exception exception)
            {
                return RegistryValueMutationResult.Error(exception.Message, value.Name, value);
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

        private static object ConvertBytesToValue(RegistryValueKind kind, byte[] data)
        {
            byte[] bytes = data ?? new byte[0];
            switch (kind)
            {
                case RegistryValueKind.Binary:
                    return bytes;
                case RegistryValueKind.MultiString:
                    return GetStringArray(bytes);
                case RegistryValueKind.DWord:
                    return bytes.Length < sizeof(uint) ? 0 : unchecked((int)BitConverter.ToUInt32(bytes, 0));
                case RegistryValueKind.QWord:
                    return bytes.Length < sizeof(ulong) ? 0L : unchecked((long)BitConverter.ToUInt64(bytes, 0));
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return Encoding.Unicode.GetString(bytes).TrimEnd('\0');
                default:
                    return new byte[0];
            }
        }

        private static string[] GetStringArray(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return new string[0];

            return Encoding.Unicode
                .GetString(bytes)
                .Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static object GetDefaultValue(RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.Binary:
                    return new byte[0];
                case RegistryValueKind.MultiString:
                    return new string[0];
                case RegistryValueKind.DWord:
                    return 0;
                case RegistryValueKind.QWord:
                    return 0L;
                case RegistryValueKind.String:
                case RegistryValueKind.ExpandString:
                    return string.Empty;
                default:
                    return null;
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

        [SupportedOSPlatform("windows")]
        private static RegistryKey OpenWritableKey(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Invalid rootkey, could not be found.");

            string normalizedPath = NormalizeRootName(path);
            RegistryKey root = OpenRootKey(normalizedPath);
            if (root.Name == normalizedPath)
                return root;

            string subKeyName = normalizedPath.Substring(root.Name.Length + 1);
            RegistryKey subKey = root.OpenSubKey(subKeyName, true);
            root.Dispose();
            return subKey;
        }

        [SupportedOSPlatform("windows")]
        private static bool ContainsSubKey(RegistryKey key, string subKeyName)
        {
            return Array.IndexOf(key.GetSubKeyNames(), subKeyName) >= 0;
        }

        [SupportedOSPlatform("windows")]
        private static bool ContainsValue(RegistryKey key, string valueName)
        {
            return Array.IndexOf(key.GetValueNames(), valueName ?? string.Empty) >= 0;
        }

        [SupportedOSPlatform("windows")]
        private static void CopySubKey(RegistryKey parentKey, string oldName, string newName)
        {
            using (RegistryKey sourceKey = parentKey.OpenSubKey(oldName, false))
            using (RegistryKey destinationKey = parentKey.CreateSubKey(newName, true))
            {
                if (sourceKey == null || destinationKey == null)
                    throw new InvalidOperationException(RegistryKeyRenameError);

                RecursiveCopyKey(sourceKey, destinationKey);
            }
        }

        [SupportedOSPlatform("windows")]
        private static void RecursiveCopyKey(RegistryKey sourceKey, RegistryKey destinationKey)
        {
            foreach (string valueName in sourceKey.GetValueNames())
            {
                destinationKey.SetValue(valueName, sourceKey.GetValue(valueName), sourceKey.GetValueKind(valueName));
            }

            foreach (string subKeyName in sourceKey.GetSubKeyNames())
            {
                using (RegistryKey sourceSubKey = sourceKey.OpenSubKey(subKeyName, false))
                using (RegistryKey destinationSubKey = destinationKey.CreateSubKey(subKeyName, true))
                {
                    if (sourceSubKey != null && destinationSubKey != null)
                        RecursiveCopyKey(sourceSubKey, destinationSubKey);
                }
            }
        }

        private static void ValidateChildKeyName(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName))
                throw new ArgumentException("Registry key name is required.", nameof(keyName));
            if (keyName.IndexOf('\\') >= 0)
                throw new ArgumentException("Registry key name must be a child name, not a path.", nameof(keyName));
        }

        private static void ValidateValueName(string valueName)
        {
            if (valueName == null)
                throw new ArgumentException("Registry value name is required.", nameof(valueName));
            if (valueName.IndexOf('\\') >= 0)
                throw new ArgumentException("Registry value name must be a value name, not a path.", nameof(valueName));
        }

        private static bool IsDefaultValue(string valueName)
        {
            return string.IsNullOrEmpty(valueName);
        }

        private static string GetWriteAccessError(string parentPath)
        {
            return $"You do not have write access to registry: {parentPath}, try running client as administrator";
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
#pragma warning restore CA1416
}
