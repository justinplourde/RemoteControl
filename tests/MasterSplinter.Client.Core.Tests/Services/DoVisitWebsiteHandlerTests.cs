using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.Services;
using MasterSplinter.Common.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.Services
{
    [TestClass]
    public class DoVisitWebsiteHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerVisitsWebsiteAndReturnsLegacyStatus()
        {
            var provider = new RecordingWebsiteVisitProvider();
            var handler = new DoVisitWebsiteHandler(provider);

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoVisitWebsite
                {
                    Url = "https://example.test",
                    Hidden = true
                },
                CancellationToken.None);

            Assert.AreEqual("https://example.test", provider.Url);
            Assert.IsTrue(provider.Hidden);
            Assert.AreEqual("Visited Website", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsFailureStatusWhenProviderFails()
        {
            var handler = new DoVisitWebsiteHandler(new FailingWebsiteVisitProvider());

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoVisitWebsite { Url = "https://example.test" },
                CancellationToken.None);

            Assert.AreEqual("Visit Website failed: boom", response.Message);
        }

        private sealed class RecordingWebsiteVisitProvider : IWebsiteVisitProvider
        {
            public string Url { get; private set; }

            public bool Hidden { get; private set; }

            public void Visit(string url, bool hidden)
            {
                Url = url;
                Hidden = hidden;
            }
        }

        private sealed class FailingWebsiteVisitProvider : IWebsiteVisitProvider
        {
            public void Visit(string url, bool hidden)
            {
                throw new InvalidOperationException("boom");
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
    }
}
