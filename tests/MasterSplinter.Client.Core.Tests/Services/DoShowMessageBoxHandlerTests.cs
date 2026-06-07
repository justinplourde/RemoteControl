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
    public class DoShowMessageBoxHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerShowsMessageBoxAndReturnsLegacyStatus()
        {
            var provider = new RecordingMessageBoxProvider();
            var handler = new DoShowMessageBoxHandler(provider);

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoShowMessageBox
                {
                    Caption = "Notice",
                    Text = "Hello",
                    Button = "OKCancel",
                    Icon = "Information"
                },
                CancellationToken.None);

            Assert.AreEqual("Hello", provider.Text);
            Assert.AreEqual("Notice", provider.Caption);
            Assert.AreEqual("OKCancel", provider.Button);
            Assert.AreEqual("Information", provider.Icon);
            Assert.AreEqual("Successfully displayed MessageBox", response.Message);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task HandlerReturnsFailureStatusWhenProviderFails()
        {
            var handler = new DoShowMessageBoxHandler(new FailingMessageBoxProvider());

            var response = (SetStatus)await handler.HandleAsync(
                new TestClientContext("client-1"),
                new DoShowMessageBox { Text = "Hello" },
                CancellationToken.None);

            Assert.AreEqual("MessageBox failed: boom", response.Message);
        }

        private sealed class RecordingMessageBoxProvider : IMessageBoxProvider
        {
            public string Text { get; private set; }

            public string Caption { get; private set; }

            public string Button { get; private set; }

            public string Icon { get; private set; }

            public void Show(string text, string caption, string button, string icon)
            {
                Text = text;
                Caption = caption;
                Button = button;
                Icon = icon;
            }
        }

        private sealed class FailingMessageBoxProvider : IMessageBoxProvider
        {
            public void Show(string text, string caption, string button, string icon)
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
