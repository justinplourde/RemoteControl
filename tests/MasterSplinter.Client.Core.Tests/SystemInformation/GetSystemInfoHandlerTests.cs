using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.SystemInformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MasterSplinter.Common.Messages;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.SystemInformation
{
    [TestClass]
    public class GetSystemInfoHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsSystemInfoResponseFromProvider()
        {
            var provider = new TestSystemInfoProvider(new[]
            {
                Tuple.Create("PC Name", "modern-client"),
                Tuple.Create("Country", "XX")
            });
            var handler = new GetSystemInfoHandler(provider);

            var response = (GetSystemInfoResponse)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new GetSystemInfo(),
                CancellationToken.None);

            Assert.AreEqual(2, response.SystemInfos.Count);
            Assert.AreEqual("PC Name", response.SystemInfos[0].Item1);
            Assert.AreEqual("modern-client", response.SystemInfos[0].Item2);
            Assert.AreEqual("Country", response.SystemInfos[1].Item1);
            Assert.AreEqual("XX", response.SystemInfos[1].Item2);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterSendsHandlerResponseThroughCommandContext()
        {
            var provider = new TestSystemInfoProvider(new[]
            {
                Tuple.Create("Username", "operator")
            });
            var adapter = new ResponseMessageHandlerAdapter<GetSystemInfo>(
                new GetSystemInfoHandler(provider));
            var context = new RecordingCommandContext("client-1");

            await adapter.HandleAsync(context, new GetSystemInfo(), CancellationToken.None);

            Assert.IsInstanceOfType(context.SentMessages[0], typeof(GetSystemInfoResponse));
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ResponseAdapterRequiresCommandContext()
        {
            var adapter = new ResponseMessageHandlerAdapter<GetSystemInfo>(
                new GetSystemInfoHandler(new TestSystemInfoProvider(Array.Empty<Tuple<string, string>>())));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                adapter.HandleAsync(new TestClientContext("client-1"), new GetSystemInfo(), CancellationToken.None));
        }

        [TestMethod, TestCategory("ClientCore")]
        public void SystemInfoProviderReturnsLegacyFieldOrderFromComposedProviders()
        {
            var provider = new SystemInfoProvider(
                new TestLocalSystemInfoProvider(new LocalSystemInfo
                {
                    CpuName = "CPU",
                    MemoryRam = "32768 MB",
                    GpuName = "GPU",
                    Username = "user",
                    PcName = "pc",
                    DomainName = "domain",
                    HostName = "host",
                    SystemDrive = "C:\\",
                    SystemDirectory = "C:\\Windows\\system32",
                    Uptime = "1d : 2h : 3m : 4s",
                    MacAddress = "AA:BB:CC:DD:EE:FF",
                    LanIpAddress = "192.168.1.10",
                    Antivirus = "Defender",
                    Firewall = "Firewall",
                    TimeZone = "UTC"
                }),
                new TestPublicNetworkInfoProvider(new PublicNetworkInfo
                {
                    WanIpAddress = "203.0.113.10",
                    Asn = "64500",
                    Isp = "Example ISP",
                    Country = "United States"
                }));

            IReadOnlyList<Tuple<string, string>> info = provider.GetSystemInfo();

            Assert.AreEqual(19, info.Count);
            AssertEntry(info, 0, "Processor (CPU)", "CPU");
            AssertEntry(info, 1, "Memory (RAM)", "32768 MB");
            AssertEntry(info, 2, "Video Card (GPU)", "GPU");
            AssertEntry(info, 3, "Username", "user");
            AssertEntry(info, 4, "PC Name", "pc");
            AssertEntry(info, 5, "Domain Name", "domain");
            AssertEntry(info, 6, "Host Name", "host");
            AssertEntry(info, 7, "System Drive", "C:\\");
            AssertEntry(info, 8, "System Directory", "C:\\Windows\\system32");
            AssertEntry(info, 9, "Uptime", "1d : 2h : 3m : 4s");
            AssertEntry(info, 10, "MAC Address", "AA:BB:CC:DD:EE:FF");
            AssertEntry(info, 11, "LAN IP Address", "192.168.1.10");
            AssertEntry(info, 12, "WAN IP Address", "203.0.113.10");
            AssertEntry(info, 13, "ASN", "64500");
            AssertEntry(info, 14, "ISP", "Example ISP");
            AssertEntry(info, 15, "Antivirus", "Defender");
            AssertEntry(info, 16, "Firewall", "Firewall");
            AssertEntry(info, 17, "Time Zone", "UTC");
            AssertEntry(info, 18, "Country", "United States");
        }

        [TestMethod, TestCategory("ClientCore")]
        public void SystemInfoProviderUsesDashForEmptyLocalAndPublicFields()
        {
            var provider = new SystemInfoProvider(
                new TestLocalSystemInfoProvider(new LocalSystemInfo()),
                new TestPublicNetworkInfoProvider(new PublicNetworkInfo()));

            IReadOnlyList<Tuple<string, string>> info = provider.GetSystemInfo();

            AssertEntry(info, 0, "Processor (CPU)", "-");
            AssertEntry(info, 12, "WAN IP Address", "-");
            AssertEntry(info, 18, "Country", "-");
        }

        private sealed class TestSystemInfoProvider : ISystemInfoProvider
        {
            private readonly IReadOnlyList<Tuple<string, string>> _systemInfo;

            public TestSystemInfoProvider(IReadOnlyList<Tuple<string, string>> systemInfo)
            {
                _systemInfo = systemInfo;
            }

            public IReadOnlyList<Tuple<string, string>> GetSystemInfo()
            {
                return _systemInfo;
            }
        }

        private sealed class TestLocalSystemInfoProvider : ILocalSystemInfoProvider
        {
            private readonly LocalSystemInfo _systemInfo;

            public TestLocalSystemInfoProvider(LocalSystemInfo systemInfo)
            {
                _systemInfo = systemInfo;
            }

            public LocalSystemInfo GetLocalSystemInfo()
            {
                return _systemInfo;
            }
        }

        private sealed class TestPublicNetworkInfoProvider : IPublicNetworkInfoProvider
        {
            private readonly PublicNetworkInfo _publicNetworkInfo;

            public TestPublicNetworkInfoProvider(PublicNetworkInfo publicNetworkInfo)
            {
                _publicNetworkInfo = publicNetworkInfo;
            }

            public PublicNetworkInfo GetPublicNetworkInfo()
            {
                return _publicNetworkInfo;
            }
        }

        private static void AssertEntry(
            IReadOnlyList<Tuple<string, string>> entries,
            int index,
            string key,
            string value)
        {
            Assert.AreEqual(key, entries[index].Item1);
            Assert.AreEqual(value, entries[index].Item2);
        }

        private sealed class TestClientContext : IClientContext
        {
            public TestClientContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }
        }

        private sealed class RecordingCommandContext : IClientCommandContext
        {
            public RecordingCommandContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public List<IMessage> SentMessages { get; } = new List<IMessage>();

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                SentMessages.Add(message);
                return Task.CompletedTask;
            }
        }
    }
}
