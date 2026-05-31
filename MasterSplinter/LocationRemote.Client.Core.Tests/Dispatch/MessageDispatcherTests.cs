using LocationRemote.Client.Core.Dispatch;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Quasar.Common.Messages;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LocationRemote.Client.Core.Tests.Dispatch
{
    [TestClass]
    public class MessageDispatcherTests
    {
        [TestMethod, TestCategory("Dispatch")]
        public async Task DispatchesKnownMessageToTypedHandler()
        {
            var context = new TestClientContext("client-1");
            var handler = new RecordingHandler<GetSystemInfo>();
            var dispatcher = new MessageDispatcher.Builder()
                .AddHandler(handler)
                .Build();

            var result = await dispatcher.DispatchAsync(context, new GetSystemInfo(), CancellationToken.None);

            Assert.AreEqual(DispatchStatus.Handled, result.Status);
            Assert.AreSame(context, handler.Context);
            Assert.IsInstanceOfType(handler.Message, typeof(GetSystemInfo));
        }

        [TestMethod, TestCategory("Dispatch")]
        public async Task UnknownMessageReturnsUnhandled()
        {
            var dispatcher = new MessageDispatcher.Builder().Build();

            var result = await dispatcher.DispatchAsync(new TestClientContext("client-1"), new GetSystemInfo(), CancellationToken.None);

            Assert.AreEqual(DispatchStatus.Unhandled, result.Status);
            Assert.IsNull(result.Exception);
        }

        [TestMethod, TestCategory("Dispatch")]
        public async Task HandlerExceptionReturnsFaultedResult()
        {
            var exception = new InvalidOperationException("boom");
            var dispatcher = new MessageDispatcher.Builder()
                .AddHandler(new ThrowingHandler<GetSystemInfo>(exception))
                .Build();

            var result = await dispatcher.DispatchAsync(new TestClientContext("client-1"), new GetSystemInfo(), CancellationToken.None);

            Assert.AreEqual(DispatchStatus.Faulted, result.Status);
            Assert.AreSame(exception, result.Exception);
        }

        [TestMethod, TestCategory("Dispatch")]
        public void DuplicateHandlerRegistrationThrows()
        {
            var builder = new MessageDispatcher.Builder()
                .AddHandler(new RecordingHandler<GetSystemInfo>());

            Assert.ThrowsException<InvalidOperationException>(() => builder.AddHandler(new RecordingHandler<GetSystemInfo>()));
        }

        [TestMethod, TestCategory("Dispatch")]
        public async Task CancellationTokenFlowsToHandler()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                var handler = new RecordingHandler<GetSystemInfo>();
                var dispatcher = new MessageDispatcher.Builder()
                    .AddHandler(handler)
                    .Build();

                await dispatcher.DispatchAsync(new TestClientContext("client-1"), new GetSystemInfo(), tokenSource.Token);

                Assert.AreEqual(tokenSource.Token, handler.CancellationToken);
            }
        }

        [TestMethod, TestCategory("Dispatch")]
        public async Task HandlerCancellationIsPropagatedWhenTokenIsCanceled()
        {
            using (var tokenSource = new CancellationTokenSource())
            {
                tokenSource.Cancel();
                var dispatcher = new MessageDispatcher.Builder()
                    .AddHandler(new CancelingHandler<GetSystemInfo>())
                    .Build();

                await Assert.ThrowsExceptionAsync<OperationCanceledException>(() =>
                    dispatcher.DispatchAsync(new TestClientContext("client-1"), new GetSystemInfo(), tokenSource.Token));
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

        private sealed class RecordingHandler<TMessage> : IMessageHandler<TMessage> where TMessage : IMessage
        {
            public IClientContext Context { get; private set; }

            public TMessage Message { get; private set; }

            public CancellationToken CancellationToken { get; private set; }

            public Task HandleAsync(IClientContext context, TMessage message, CancellationToken cancellationToken)
            {
                Context = context;
                Message = message;
                CancellationToken = cancellationToken;
                return Task.CompletedTask;
            }
        }

        private sealed class ThrowingHandler<TMessage> : IMessageHandler<TMessage> where TMessage : IMessage
        {
            private readonly Exception _exception;

            public ThrowingHandler(Exception exception)
            {
                _exception = exception;
            }

            public Task HandleAsync(IClientContext context, TMessage message, CancellationToken cancellationToken)
            {
                throw _exception;
            }
        }

        private sealed class CancelingHandler<TMessage> : IMessageHandler<TMessage> where TMessage : IMessage
        {
            public Task HandleAsync(IClientContext context, TMessage message, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.CompletedTask;
            }
        }
    }
}
