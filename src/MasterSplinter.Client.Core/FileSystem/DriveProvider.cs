using Quasar.Common.Models;
using System;
using System.IO;
using System.Linq;

namespace MasterSplinter.Client.Core.FileSystem
{
    public sealed class DriveProvider : IDriveProvider
    {
        public DriveListResult GetDrives()
        {
            DriveInfo[] driveInfos;
            try
            {
                driveInfos = DriveInfo.GetDrives().Where(drive => drive.IsReady).ToArray();
            }
            catch (IOException)
            {
                return DriveListResult.Error("GetDrives I/O error");
            }
            catch (UnauthorizedAccessException)
            {
                return DriveListResult.Error("GetDrives No permission");
            }

            if (driveInfos.Length == 0)
                return DriveListResult.Error("GetDrives No drives");

            var drives = new Drive[driveInfos.Length];
            for (int index = 0; index < driveInfos.Length; index++)
            {
                DriveInfo drive = driveInfos[index];
                string displayName = string.IsNullOrEmpty(drive.VolumeLabel)
                    ? $"{drive.RootDirectory.FullName} [{ToFriendlyString(drive.DriveType)}, {drive.DriveFormat}]"
                    : $"{drive.RootDirectory.FullName} ({drive.VolumeLabel}) [{ToFriendlyString(drive.DriveType)}, {drive.DriveFormat}]";

                drives[index] = new Drive
                {
                    DisplayName = displayName,
                    RootDirectory = drive.RootDirectory.FullName
                };
            }

            return DriveListResult.Success(drives);
        }

        private static string ToFriendlyString(DriveType type)
        {
            switch (type)
            {
                case DriveType.Fixed:
                    return "Local Disk";
                case DriveType.Network:
                    return "Network Drive";
                case DriveType.Removable:
                    return "Removable Drive";
                default:
                    return type.ToString();
            }
        }
    }
}
