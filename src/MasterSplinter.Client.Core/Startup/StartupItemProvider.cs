using Microsoft.Win32;
using MasterSplinter.Common.Enums;
using MasterSplinter.Common.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;

namespace MasterSplinter.Client.Core.Startup
{
    public sealed class StartupItemProvider : IStartupItemProvider
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
    }
}
