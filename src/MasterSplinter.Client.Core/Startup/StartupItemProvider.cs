using Microsoft.Win32;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace MasterSplinter.Client.Core.Startup
{
#pragma warning disable CA1416
    public sealed class StartupItemProvider : IStartupItemProvider, IStartupItemMutationProvider
    {
        private const string RunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string RunOnceKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce";
        private const string RunKeyX86 = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string RunOnceKeyX86 = "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\RunOnce";

        public StartupItemsResult GetStartupItems()
        {
            try
            {
                var startupItems = new List<StartupItem>();

                if (OperatingSystem.IsWindows())
                {
                    AddRegistryItems(startupItems, RegistryHive.LocalMachine, RunKey, StartupType.LocalMachineRun);
                    AddRegistryItems(startupItems, RegistryHive.LocalMachine, RunOnceKey, StartupType.LocalMachineRunOnce);
                    AddRegistryItems(startupItems, RegistryHive.CurrentUser, RunKey, StartupType.CurrentUserRun);
                    AddRegistryItems(startupItems, RegistryHive.CurrentUser, RunOnceKey, StartupType.CurrentUserRunOnce);
                    AddRegistryItems(startupItems, RegistryHive.LocalMachine, RunKeyX86, StartupType.LocalMachineRunX86);
                    AddRegistryItems(startupItems, RegistryHive.LocalMachine, RunOnceKeyX86, StartupType.LocalMachineRunOnceX86);
                }

                string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (Directory.Exists(startupFolder))
                {
                    foreach (FileInfo file in new DirectoryInfo(startupFolder).GetFiles())
                    {
                        if (string.Equals(file.Name, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                            continue;

                        startupItems.Add(new StartupItem
                        {
                            Name = file.Name,
                            Path = file.FullName,
                            Type = StartupType.StartMenu
                        });
                    }
                }

                return StartupItemsResult.Success(startupItems);
            }
            catch (Exception exception)
            {
                return StartupItemsResult.Error($"Getting Autostart Items failed: {exception.Message}");
            }
        }

        public StartupItemMutationResult AddStartupItem(StartupItem startupItem)
        {
            try
            {
                ValidateStartupItem(startupItem, requirePath: true);
                switch (startupItem.Type)
                {
                    case StartupType.LocalMachineRun:
                        SetRegistryValue(RegistryHive.LocalMachine, RunKey, startupItem.Name, startupItem.Path);
                        break;
                    case StartupType.LocalMachineRunOnce:
                        SetRegistryValue(RegistryHive.LocalMachine, RunOnceKey, startupItem.Name, startupItem.Path);
                        break;
                    case StartupType.CurrentUserRun:
                        SetRegistryValue(RegistryHive.CurrentUser, RunKey, startupItem.Name, startupItem.Path);
                        break;
                    case StartupType.CurrentUserRunOnce:
                        SetRegistryValue(RegistryHive.CurrentUser, RunOnceKey, startupItem.Name, startupItem.Path);
                        break;
                    case StartupType.LocalMachineRunX86:
                        SetRegistryValue(RegistryHive.LocalMachine, RunKeyX86, startupItem.Name, startupItem.Path);
                        break;
                    case StartupType.LocalMachineRunOnceX86:
                        SetRegistryValue(RegistryHive.LocalMachine, RunOnceKeyX86, startupItem.Name, startupItem.Path);
                        break;
                    case StartupType.StartMenu:
                        AddStartMenuItem(startupItem);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported startup type '{startupItem.Type}'.");
                }

                return StartupItemMutationResult.Success();
            }
            catch (Exception exception)
            {
                return StartupItemMutationResult.Error($"Adding Autostart Item failed: {exception.Message}");
            }
        }

        public StartupItemMutationResult RemoveStartupItem(StartupItem startupItem)
        {
            try
            {
                ValidateStartupItem(startupItem, requirePath: false);
                switch (startupItem.Type)
                {
                    case StartupType.LocalMachineRun:
                        DeleteRegistryValue(RegistryHive.LocalMachine, RunKey, startupItem.Name);
                        break;
                    case StartupType.LocalMachineRunOnce:
                        DeleteRegistryValue(RegistryHive.LocalMachine, RunOnceKey, startupItem.Name);
                        break;
                    case StartupType.CurrentUserRun:
                        DeleteRegistryValue(RegistryHive.CurrentUser, RunKey, startupItem.Name);
                        break;
                    case StartupType.CurrentUserRunOnce:
                        DeleteRegistryValue(RegistryHive.CurrentUser, RunOnceKey, startupItem.Name);
                        break;
                    case StartupType.LocalMachineRunX86:
                        DeleteRegistryValue(RegistryHive.LocalMachine, RunKeyX86, startupItem.Name);
                        break;
                    case StartupType.LocalMachineRunOnceX86:
                        DeleteRegistryValue(RegistryHive.LocalMachine, RunOnceKeyX86, startupItem.Name);
                        break;
                    case StartupType.StartMenu:
                        RemoveStartMenuItem(startupItem);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported startup type '{startupItem.Type}'.");
                }

                return StartupItemMutationResult.Success();
            }
            catch (Exception exception)
            {
                return StartupItemMutationResult.Error($"Removing Autostart Item failed: {exception.Message}");
            }
        }

        [SupportedOSPlatform("windows")]
        private static void AddRegistryItems(
            List<StartupItem> startupItems,
            RegistryHive hive,
            string path,
            StartupType type)
        {
            using (RegistryKey key = OpenReadonlySubKey(hive, path))
            {
                if (key == null)
                    return;

                foreach (string valueName in key.GetValueNames())
                {
                    if (string.IsNullOrEmpty(valueName))
                        continue;

                    string value = string.Empty;
                    try
                    {
                        object rawValue = key.GetValue(valueName, string.Empty);
                        if (rawValue != null)
                            value = rawValue.ToString();
                    }
                    catch
                    {
                        value = string.Empty;
                    }

                    startupItems.Add(new StartupItem
                    {
                        Name = valueName,
                        Path = value,
                        Type = type
                    });
                }
            }
        }

        [SupportedOSPlatform("windows")]
        private static RegistryKey OpenReadonlySubKey(RegistryHive hive, string path)
        {
            try
            {
                return RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).OpenSubKey(path, false);
            }
            catch
            {
                return null;
            }
        }

        private static void ValidateStartupItem(StartupItem startupItem, bool requirePath)
        {
            if (startupItem == null)
                throw new ArgumentNullException(nameof(startupItem));
            if (string.IsNullOrWhiteSpace(startupItem.Name))
                throw new ArgumentException("Startup item name is required.", nameof(startupItem));
            if (requirePath && string.IsNullOrWhiteSpace(startupItem.Path))
                throw new ArgumentException("Startup item path is required.", nameof(startupItem));
        }

        [SupportedOSPlatform("windows")]
        private static void SetRegistryValue(RegistryHive hive, string path, string name, string value)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Startup registry writes are only supported on Windows.");

            using (RegistryKey key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).CreateSubKey(path, true))
            {
                if (key == null)
                    throw new InvalidOperationException("Could not open startup registry key.");

                key.SetValue(name, value);
            }
        }

        [SupportedOSPlatform("windows")]
        private static void DeleteRegistryValue(RegistryHive hive, string path, string name)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Startup registry writes are only supported on Windows.");

            using (RegistryKey key = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64).OpenSubKey(path, true))
            {
                if (key == null || Array.IndexOf(key.GetValueNames(), name) < 0)
                    throw new InvalidOperationException("Could not remove value");

                key.DeleteValue(name);
            }
        }

        private static void AddStartMenuItem(StartupItem startupItem)
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            if (!Directory.Exists(startupFolder))
                Directory.CreateDirectory(startupFolder);

            string fileName = startupItem.Name.EndsWith(".url", StringComparison.OrdinalIgnoreCase)
                ? startupItem.Name
                : startupItem.Name + ".url";
            string shortcutPath = Path.Combine(startupFolder, fileName);
            using (var writer = new StreamWriter(shortcutPath, false))
            {
                writer.WriteLine("[InternetShortcut]");
                writer.WriteLine("URL=file:///" + startupItem.Path);
                writer.WriteLine("IconIndex=0");
                writer.WriteLine("IconFile=" + startupItem.Path.Replace('\\', '/'));
            }
        }

        private static void RemoveStartMenuItem(StartupItem startupItem)
        {
            string startupFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string startupItemPath = Path.Combine(startupFolder, startupItem.Name);
            if (!File.Exists(startupItemPath) && !startupItem.Name.EndsWith(".url", StringComparison.OrdinalIgnoreCase))
                startupItemPath = Path.Combine(startupFolder, startupItem.Name + ".url");
            if (!File.Exists(startupItemPath))
                throw new IOException("File does not exist");

            File.Delete(startupItemPath);
        }
    }
#pragma warning restore CA1416
}
