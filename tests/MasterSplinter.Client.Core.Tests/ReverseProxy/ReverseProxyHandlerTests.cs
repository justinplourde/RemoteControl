using MasterSplinter.Client.Core.Dispatch;
using MasterSplinter.Client.Core.ReverseProxy;
using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Messages.ReverseProxy;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Client.Core.Tests.ReverseProxy
{
    [TestClass]
    public class ReverseProxyHandlerTests
    {
        [TestMethod, TestCategory("ClientCore")]
        public async Task ConnectHandlerRequiresCommandContextAndDelegates()
        {
            var provider = new TestReverseProxyProvider();
            var handler = new ReverseProxyConnectHandler(provider);
            var context = new TestCommandContext("client-1");
            var message = new ReverseProxyConnect
            {
                ConnectionId = 7,
                Target = "127.0.0.1",
                Port = 8080
            };

            await handler.HandleAsync(context, message, CancellationToken.None);

            Assert.AreSame(message, provider.ConnectMessage);
            Assert.AreEqual(1, context.Sent.Count);
            Assert.IsInstanceOfType(context.Sent[0], typeof(ReverseProxyConnectResponse));
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task DataHandlerDelegates()
        {
            var provider = new TestReverseProxyProvider();
            var handler = new ReverseProxyDataHandler(provider);
            var message = new ReverseProxyData
            {
                ConnectionId = 7,
                Data = new byte[] { 1, 2, 3 }
            };

            await handler.HandleAsync(new TestCommandContext("client-1"), message, CancellationToken.None);

            Assert.AreSame(message, provider.DataMessage);
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task DisconnectHandlerDelegates()
        {
            var provider = new TestReverseProxyProvider();
            var handler = new ReverseProxyDisconnectHandler(provider);
            var message = new ReverseProxyDisconnect { ConnectionId = 7 };

            await handler.HandleAsync(new TestCommandContext("client-1"), message, CancellationToken.None);

            Assert.AreSame(message, provider.DisconnectMessage);
        }

        [TestMethod, TestCategory("ClientCore")]
        public void LoopbackPolicyAllowsOnlyLoopbackTargets()
        {
            IReverseProxyTargetPolicy policy = ReverseProxyTargetPolicy.LoopbackOnly();

            Assert.IsTrue(policy.IsAllowed("127.0.0.1", 80));
            Assert.IsTrue(policy.IsAllowed("::1", 80));
            Assert.IsTrue(policy.IsAllowed("localhost", 80));
            Assert.IsFalse(policy.IsAllowed("example.com", 80));
            Assert.IsFalse(policy.IsAllowed("8.8.8.8", 53));
            Assert.IsFalse(policy.IsAllowed("127.0.0.1", 0));
        }

        [TestMethod, TestCategory("ClientCore")]
        public async Task ProviderConnectsToAllowedLoopbackTargetAndRelaysData()
        {
            using var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            Task echoTask = EchoOnceAsync(listener);

            using var provider = new ReverseProxyProvider(ReverseProxyTargetPolicy.LoopbackOnly());
            var sent = new List<IMessage>();
            await provider.ConnectAsync(
                new ReverseProxyConnect
                {
                    ConnectionId = 42,
                    Target = "127.0.0.1",
                    Port = port
                },
                (message, token) =>
                {
                    sent.Add(message);
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            var response = (ReverseProxyConnectResponse)sent[0];
            Assert.IsTrue(response.IsConnected);

            await provider.SendDataAsync(
                new ReverseProxyData
                {
                    ConnectionId = 42,
                    Data = new byte[] { 9, 8, 7 }
                },
                (message, token) =>
                {
                    sent.Add(message);
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            await WaitForMessageAsync<ReverseProxyData>(sent, TimeSpan.FromSeconds(5));
            await provider.DisconnectAsync(
                new ReverseProxyDisconnect { ConnectionId = 42 },
                (message, token) =>
                {
                    sent.Add(message);
                    return Task.CompletedTask;
                },
                CancellationToken.None);
            await echoTask;
        }

        private sealed class TestReverseProxyProvider : IReverseProxyProvider
        {
            public ReverseProxyConnect ConnectMessage { get; private set; }

            public ReverseProxyData DataMessage { get; private set; }

            public ReverseProxyDisconnect DisconnectMessage { get; private set; }

            public async Task ConnectAsync(
                ReverseProxyConnect message,
                Func<IMessage, CancellationToken, Task> sendAsync,
                CancellationToken cancellationToken)
            {
                ConnectMessage = message;
                await sendAsync(new ReverseProxyConnectResponse
                {
                    ConnectionId = message.ConnectionId,
                    IsConnected = true,
                    HostName = message.Target
                }, cancellationToken);
            }

            public Task SendDataAsync(
                ReverseProxyData message,
                Func<IMessage, CancellationToken, Task> sendAsync,
                CancellationToken cancellationToken)
            {
                DataMessage = message;
                return Task.CompletedTask;
            }

            public Task DisconnectAsync(
                ReverseProxyDisconnect message,
                Func<IMessage, CancellationToken, Task> sendAsync,
                CancellationToken cancellationToken)
            {
                DisconnectMessage = message;
                return Task.CompletedTask;
            }
        }

        private sealed class TestCommandContext : IClientCommandContext
        {
            public TestCommandContext(string clientId)
            {
                ClientId = clientId;
            }

            public string ClientId { get; }

            public List<IMessage> Sent { get; } = new List<IMessage>();

            public Task SendAsync(IMessage message, CancellationToken cancellationToken)
            {
                Sent.Add(message);
                return Task.CompletedTask;
            }
        }

        private static async Task EchoOnceAsync(TcpListener listener)
        {
            using TcpClient client = await listener.AcceptTcpClientAsync();
            var buffer = new byte[3];
            int received = await client.GetStream().ReadAsync(buffer, CancellationToken.None);
            await client.GetStream().WriteAsync(buffer.AsMemory(0, received), CancellationToken.None);
        }

        private static async Task<TMessage> WaitForMessageAsync<TMessage>(
            List<IMessage> messages,
            TimeSpan timeout)
            where TMessage : IMessage
        {
            DateTimeOffset deadline = DateTimeOffset.UtcNow + timeout;
            while (DateTimeOffset.UtcNow < deadline)
            {
                foreach (IMessage message in messages)
                {
                    if (message is TMessage typed)
                        return typed;
                }

                await Task.Delay(50);
            }

            Assert.Fail($"Timed out waiting for {typeof(TMessage).Name}.");
            return default;
        }
    }
}
