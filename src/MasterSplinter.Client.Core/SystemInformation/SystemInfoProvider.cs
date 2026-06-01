using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;

namespace MasterSplinter.Client.Core.SystemInformation
{
    public sealed class SystemInfoProvider : ISystemInfoProvider
    {
        public IReadOnlyList<Tuple<string, string>> GetSystemInfo()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            string domainName = string.IsNullOrWhiteSpace(properties.DomainName) ? "-" : properties.DomainName;
            string hostName = string.IsNullOrWhiteSpace(properties.HostName) ? "-" : properties.HostName;

            return new List<Tuple<string, string>>
            {
                Tuple.Create("Processor (CPU)", "-"),
                Tuple.Create("Memory (RAM)", "-"),
                Tuple.Create("Video Card (GPU)", "-"),
                Tuple.Create("Username", Environment.UserName),
                Tuple.Create("PC Name", Environment.MachineName),
                Tuple.Create("Domain Name", domainName),
                Tuple.Create("Host Name", hostName),
                Tuple.Create("System Drive", Path.GetPathRoot(Environment.SystemDirectory) ?? "-"),
                Tuple.Create("System Directory", Environment.SystemDirectory),
                Tuple.Create("Uptime", "-"),
                Tuple.Create("MAC Address", "-"),
                Tuple.Create("LAN IP Address", "-"),
                Tuple.Create("WAN IP Address", "-"),
                Tuple.Create("ASN", "-"),
                Tuple.Create("ISP", "-"),
                Tuple.Create("Antivirus", "-"),
                Tuple.Create("Firewall", "-"),
                Tuple.Create("Time Zone", TimeZoneInfo.Local.DisplayName),
                Tuple.Create("Country", "-")
            };
        }
    }
}
