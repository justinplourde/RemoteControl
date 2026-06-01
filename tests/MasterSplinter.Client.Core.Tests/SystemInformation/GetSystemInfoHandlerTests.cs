using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.SystemInformation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
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
