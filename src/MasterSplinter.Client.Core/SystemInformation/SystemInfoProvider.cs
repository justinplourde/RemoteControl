using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace MasterSplinter.Client.Core.SystemInformation
{
    public sealed class SystemInfoProvider : ISystemInfoProvider
    {
        private readonly ILocalSystemInfoProvider _localProvider;
        private readonly IPublicNetworkInfoProvider _publicNetworkProvider;

        public SystemInfoProvider()
            : this(new LocalSystemInfoProvider(), new PublicNetworkInfoProvider())
        {
        }

        public SystemInfoProvider(
            ILocalSystemInfoProvider localProvider,
            IPublicNetworkInfoProvider publicNetworkProvider)
        {
            _localProvider = localProvider ?? throw new ArgumentNullException(nameof(localProvider));
            _publicNetworkProvider = publicNetworkProvider ?? throw new ArgumentNullException(nameof(publicNetworkProvider));
        }

        public IReadOnlyList<Tuple<string, string>> GetSystemInfo()
        {
            LocalSystemInfo local = _localProvider.GetLocalSystemInfo();
            PublicNetworkInfo publicNetwork = _publicNetworkProvider.GetPublicNetworkInfo();

            return new List<Tuple<string, string>>
            {
                Tuple.Create("Processor (CPU)", ValueOrDash(local.CpuName)),
                Tuple.Create("Memory (RAM)", ValueOrDash(local.MemoryRam)),
                Tuple.Create("Video Card (GPU)", ValueOrDash(local.GpuName)),
                Tuple.Create("Username", ValueOrDash(local.Username)),
                Tuple.Create("PC Name", ValueOrDash(local.PcName)),
                Tuple.Create("Domain Name", ValueOrDash(local.DomainName)),
                Tuple.Create("Host Name", ValueOrDash(local.HostName)),
                Tuple.Create("System Drive", ValueOrDash(local.SystemDrive)),
                Tuple.Create("System Directory", ValueOrDash(local.SystemDirectory)),
                Tuple.Create("Uptime", ValueOrDash(local.Uptime)),
                Tuple.Create("MAC Address", ValueOrDash(local.MacAddress)),
                Tuple.Create("LAN IP Address", ValueOrDash(local.LanIpAddress)),
                Tuple.Create("WAN IP Address", ValueOrDash(publicNetwork.WanIpAddress)),
                Tuple.Create("ASN", ValueOrDash(publicNetwork.Asn)),
                Tuple.Create("ISP", ValueOrDash(publicNetwork.Isp)),
                Tuple.Create("Antivirus", ValueOrDash(local.Antivirus)),
                Tuple.Create("Firewall", ValueOrDash(local.Firewall)),
                Tuple.Create("Time Zone", ValueOrDash(local.TimeZone)),
                Tuple.Create("Country", ValueOrDash(publicNetwork.Country))
            };
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "-" : value;
        }
    }

    public interface ILocalSystemInfoProvider
    {
        LocalSystemInfo GetLocalSystemInfo();
    }

    public interface IPublicNetworkInfoProvider
    {
        PublicNetworkInfo GetPublicNetworkInfo();
    }

    public sealed class LocalSystemInfo
    {
        public string CpuName { get; set; }
        public string MemoryRam { get; set; }
        public string GpuName { get; set; }
        public string Username { get; set; }
        public string PcName { get; set; }
        public string DomainName { get; set; }
        public string HostName { get; set; }
        public string SystemDrive { get; set; }
        public string SystemDirectory { get; set; }
        public string Uptime { get; set; }
        public string MacAddress { get; set; }
        public string LanIpAddress { get; set; }
        public string Antivirus { get; set; }
        public string Firewall { get; set; }
        public string TimeZone { get; set; }
    }

    public sealed class PublicNetworkInfo
    {
        public string WanIpAddress { get; set; }
        public string Asn { get; set; }
        public string Isp { get; set; }
        public string Country { get; set; }
    }

    public sealed class LocalSystemInfoProvider : ILocalSystemInfoProvider
    {
        public LocalSystemInfo GetLocalSystemInfo()
        {
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface adapter = GetPrimaryAdapter();
            string lanIpAddress = GetLanIpAddress(adapter);

            return new LocalSystemInfo
            {
                CpuName = JoinWmiValues("Win32_Processor", "Name", "Unknown"),
                MemoryRam = GetMemoryRam(),
                GpuName = GetGpuName(),
                Username = Environment.UserName,
                PcName = Environment.MachineName,
                DomainName = FirstNonEmpty(properties.DomainName, Environment.UserDomainName, "-"),
                HostName = FirstNonEmpty(properties.HostName, Dns.GetHostName(), "-"),
                SystemDrive = Path.GetPathRoot(Environment.SystemDirectory) ?? "-",
                SystemDirectory = Environment.SystemDirectory,
                Uptime = FormatUptime(TimeSpan.FromMilliseconds(Environment.TickCount64)),
                MacAddress = GetMacAddress(adapter),
                LanIpAddress = lanIpAddress,
                Antivirus = GetSecurityProducts("AntivirusProduct"),
                Firewall = GetSecurityProducts("FirewallProduct"),
                TimeZone = TimeZoneInfo.Local.DisplayName
            };
        }

        private static string GetMemoryRam()
        {
            string bytes = FirstWmiValue("Win32_ComputerSystem", "TotalPhysicalMemory");
            if (long.TryParse(bytes, NumberStyles.Integer, CultureInfo.InvariantCulture, out long totalBytes) &&
                totalBytes > 0)
            {
                return $"{totalBytes / 1048576L} MB";
            }

            return "-";
        }

        private static string GetGpuName()
        {
            string gpu = JoinWmiValues("Win32_VideoController", "Name", null);
            if (!string.IsNullOrWhiteSpace(gpu))
                return gpu;

            return JoinWmiValues("Win32_DisplayConfiguration", "Description", "Unknown");
        }

        private static string GetSecurityProducts(string className)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "-";

            string products = JoinWmiValues(
                "root\\SecurityCenter2",
                className,
                "displayName",
                "N/A");
            if (!IsEmptyProductValue(products))
                return products;

            if (string.Equals(className, "FirewallProduct", StringComparison.OrdinalIgnoreCase))
                return GetWindowsFirewallFallback();

            return JoinWmiValues(
                "root\\SecurityCenter",
                className,
                "displayName",
                "N/A");
        }

        private static bool IsEmptyProductValue(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                string.Equals(value, "-", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "N/A", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetWindowsFirewallFallback()
        {
            string state = FirstWmiValue("Win32_Service WHERE Name='MpsSvc'", "State");
            if (string.IsNullOrWhiteSpace(state))
                return "N/A";

            return $"Windows Defender Firewall ({state})";
        }

        private static string JoinWmiValues(string className, string propertyName, string fallback)
        {
            return JoinWmiValues(null, className, propertyName, fallback);
        }

        private static string JoinWmiValues(string scope, string className, string propertyName, string fallback)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return fallback ?? "-";

            try
            {
                var values = new List<string>();
                string query = $"SELECT {propertyName} FROM {className}";
                using (var searcher = string.IsNullOrWhiteSpace(scope)
                    ? new ManagementObjectSearcher(query)
                    : new ManagementObjectSearcher(scope, query))
                {
                    foreach (ManagementObject managementObject in searcher.Get())
                    {
                        string value = Convert.ToString(managementObject[propertyName], CultureInfo.InvariantCulture);
                        if (!string.IsNullOrWhiteSpace(value))
                            values.Add(value.Trim());
                    }
                }

                return values.Count == 0 ? fallback ?? "-" : string.Join("; ", values);
            }
            catch
            {
                return fallback ?? "-";
            }
        }

        private static string FirstWmiValue(string className, string propertyName)
        {
            string values = JoinWmiValues(className, propertyName, null);
            if (string.IsNullOrWhiteSpace(values))
                return null;

            int separator = values.IndexOf(';');
            return separator < 0 ? values : values.Substring(0, separator);
        }

        private static NetworkInterface GetPrimaryAdapter()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces()
                    .Where(IsCandidateAdapter)
                    .FirstOrDefault(adapter => GetLanIpAddress(adapter) != "-");
            }
            catch
            {
                return null;
            }
        }

        private static bool IsCandidateAdapter(NetworkInterface adapter)
        {
            if (adapter == null || adapter.OperationalStatus != OperationalStatus.Up)
                return false;

            if (adapter.NetworkInterfaceType != NetworkInterfaceType.Ethernet &&
                adapter.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
                return false;

            try
            {
                return adapter.GetIPProperties().GatewayAddresses.Any();
            }
            catch
            {
                return false;
            }
        }

        private static string GetLanIpAddress(NetworkInterface adapter)
        {
            if (adapter == null)
                return "-";

            try
            {
                foreach (UnicastIPAddressInformation address in adapter.GetIPProperties().UnicastAddresses)
                {
                    if (address.Address.AddressFamily == AddressFamily.InterNetwork &&
                        !IPAddress.IsLoopback(address.Address) &&
                        IsPreferredAddress(address))
                    {
                        return address.Address.ToString();
                    }
                }
            }
            catch
            {
            }

            return "-";
        }

        private static bool IsPreferredAddress(UnicastIPAddressInformation address)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true;

            try
            {
                return address.AddressPreferredLifetime != uint.MaxValue;
            }
            catch
            {
                return true;
            }
        }

        private static string GetMacAddress(NetworkInterface adapter)
        {
            if (adapter == null)
                return "-";

            try
            {
                byte[] bytes = adapter.GetPhysicalAddress().GetAddressBytes();
                return bytes.Length == 0 ? "-" : string.Join(":", bytes.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
            }
            catch
            {
                return "-";
            }
        }

        private static string FormatUptime(TimeSpan uptime)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}d : {1}h : {2}m : {3}s",
                Math.Max(0, uptime.Days),
                Math.Max(0, uptime.Hours),
                Math.Max(0, uptime.Minutes),
                Math.Max(0, uptime.Seconds));
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return "-";
        }
    }

    public sealed class PublicNetworkInfoProvider : IPublicNetworkInfoProvider
    {
        private static readonly HttpClient Client = CreateClient();

        public PublicNetworkInfo GetPublicNetworkInfo()
        {
            PublicNetworkInfo online = TryGetOnlineInfo();
            if (online != null)
                return online;

            return GetLocalFallback();
        }

        private static HttpClient CreateClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(5)
            };
        }

        private static PublicNetworkInfo TryGetOnlineInfo()
        {
            try
            {
                using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://ipwho.is/"))
                {
                    request.Headers.UserAgent.ParseAdd("MasterSplinter/1.0");
                    using (HttpResponseMessage response = Client.Send(request))
                    {
                        if (!response.IsSuccessStatusCode)
                            return null;

                        string json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                        return ParseIpWhoIs(json);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static PublicNetworkInfo ParseIpWhoIs(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                JsonElement root = document.RootElement;
                string ip = GetString(root, "ip");
                string country = GetString(root, "country");
                string asn = null;
                string isp = null;

                if (root.TryGetProperty("connection", out JsonElement connection))
                {
                    asn = GetPropertyAsString(connection, "asn");
                    isp = GetString(connection, "isp");
                }

                return new PublicNetworkInfo
                {
                    WanIpAddress = ValueOrUnknown(ip),
                    Asn = ValueOrUnknown(asn),
                    Isp = ValueOrUnknown(isp),
                    Country = ValueOrUnknown(country)
                };
            }
        }

        private static PublicNetworkInfo GetLocalFallback()
        {
            string country = null;
            try
            {
                country = new RegionInfo(CultureInfo.CurrentUICulture.LCID).DisplayName;
            }
            catch
            {
            }

            return new PublicNetworkInfo
            {
                WanIpAddress = "Unknown",
                Asn = "Unknown",
                Isp = "Unknown",
                Country = ValueOrUnknown(country)
            };
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out JsonElement property) &&
                property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }

        private static string GetPropertyAsString(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out JsonElement property))
                return null;

            if (property.ValueKind == JsonValueKind.String)
                return property.GetString();
            if (property.ValueKind == JsonValueKind.Number)
                return property.ToString();

            return null;
        }

        private static string ValueOrUnknown(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }
    }
}
