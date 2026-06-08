using MasterSplinter.Common.Messages;
using MasterSplinter.Common.Video;
using MasterSplinter.Server.Core.Commands;
using MasterSplinter.Server.Core.RemoteDesktop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.Tests.RemoteDesktop
{
    [TestClass]
    public class RemoteDesktopStreamSessionTests
    {
        [TestMethod, TestCategory("ServerCore")]
        public async Task RunAsyncRequestsFirstFrameAsNewAndContinuesRemainingFrames()
        {
            var dispatcher = new RecordingDispatcher();
            var responses = new QueueResponseSource(
                CreateFrameResponse(1),
                CreateFrameResponse(2),
                CreateFrameResponse(3));
            var createdMessages = new List<GetDesktop>();
            var session = CreateSession(dispatcher, responses, createdMessages);
            var frames = new List<RemoteDesktopStreamFrame>();

            RemoteDesktopStreamResult result = await session.RunAsync(
                CreateOptions(frameCount: 3),
                (frame, token) =>
                {
                    frames.Add(frame);
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            Assert.IsTrue(result.Completed);
            Assert.AreEqual(3, result.ReceivedFrames);
            Assert.AreEqual(3, frames.Count);
            Assert.AreEqual(3, dispatcher.Requests.Count);
            Assert.IsTrue(createdMessages[0].CreateNew);
            Assert.IsFalse(createdMessages[1].CreateNew);
            Assert.IsFalse(createdMessages[2].CreateNew);
            Assert.AreEqual(60, createdMessages[0].Quality);
            Assert.AreEqual(1, createdMessages[0].DisplayIndex);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task RunAsyncStopsAndCancelsWaitWhenDispatchIsDenied()
        {
            var dispatcher = new RecordingDispatcher
            {
                NextResult = CommandDispatchResult.Denied(
                    Guid.NewGuid(),
                    CommandDispatchStatus.PermissionDenied,
                    DefaultCommandSafetyClassifier.Instance.Classify(new GetDesktop()),
                    "Denied")
            };
            var responses = new QueueResponseSource(CreateFrameResponse(1));
            var session = CreateSession(dispatcher, responses, new List<GetDesktop>());

            RemoteDesktopStreamResult result = await session.RunAsync(
                CreateOptions(frameCount: 3),
                (frame, token) => throw new InvalidOperationException("Frame handler should not run."),
                CancellationToken.None);

            Assert.AreEqual(CommandDispatchStatus.PermissionDenied, result.LastDispatchResult.Status);
            Assert.AreEqual(0, result.ReceivedFrames);
            Assert.IsTrue(responses.CancelCalled);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task RunAsyncStopsAfterEmptyFrame()
        {
            var dispatcher = new RecordingDispatcher();
            var responses = new QueueResponseSource(
                CreateFrameResponse(1),
                new GetDesktopResponse
                {
                    Image = null,
                    Monitor = 1,
                    Quality = 60,
                    Resolution = new Resolution { Width = 1280, Height = 720 }
                },
                CreateFrameResponse(3));
            var session = CreateSession(dispatcher, responses, new List<GetDesktop>());
            var frames = new List<RemoteDesktopStreamFrame>();

            RemoteDesktopStreamResult result = await session.RunAsync(
                CreateOptions(frameCount: 3),
                (frame, token) =>
                {
                    frames.Add(frame);
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            Assert.AreEqual(2, result.ReceivedFrames);
            Assert.IsTrue(result.StoppedOnEmptyFrame);
            Assert.AreEqual(2, dispatcher.Requests.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public async Task RunAsyncIgnoresUnrelatedResponsesWhileWaitingForFrame()
        {
            var dispatcher = new RecordingDispatcher();
            var responses = new QueueResponseSource(
                new SetStatus { Message = "Mouse event sent." },
                CreateFrameResponse(1));
            var session = CreateSession(dispatcher, responses, new List<GetDesktop>());
            var frames = new List<RemoteDesktopStreamFrame>();

            RemoteDesktopStreamResult result = await session.RunAsync(
                CreateOptions(frameCount: 1),
                (frame, token) =>
                {
                    frames.Add(frame);
                    return Task.CompletedTask;
                },
                CancellationToken.None);

            Assert.AreEqual(1, result.ReceivedFrames);
            Assert.AreEqual(1, frames.Count);
        }

        [TestMethod, TestCategory("ServerCore")]
        public void OptionsRejectInvalidValues()
        {
            Assert.ThrowsException<ArgumentException>(() =>
                new RemoteDesktopStreamOptions("", 60, 0, 1, 0, TimeSpan.FromSeconds(1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new RemoteDesktopStreamOptions("client-1", 0, 0, 1, 0, TimeSpan.FromSeconds(1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new RemoteDesktopStreamOptions("client-1", 60, -1, 1, 0, TimeSpan.FromSeconds(1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new RemoteDesktopStreamOptions("client-1", 60, 0, 0, 0, TimeSpan.FromSeconds(1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new RemoteDesktopStreamOptions("client-1", 60, 0, 1, -1, TimeSpan.FromSeconds(1)));
            Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
                new RemoteDesktopStreamOptions("client-1", 60, 0, 1, 0, TimeSpan.Zero));
        }

        private static RemoteDesktopStreamSession CreateSession(
            RecordingDispatcher dispatcher,
            QueueResponseSource responses,
            List<GetDesktop> createdMessages)
        {
            return new RemoteDesktopStreamSession(
                dispatcher,
                responses,
                (message, token) =>
                {
                    createdMessages.Add(message);
                    return Task.FromResult(CommandDispatchRequest.Create("client-1", message));
                });
        }

        private static RemoteDesktopStreamOptions CreateOptions(int frameCount)
        {
            return new RemoteDesktopStreamOptions(
                "client-1",
                60,
                1,
                frameCount,
                0,
                TimeSpan.FromSeconds(1));
        }

        private static GetDesktopResponse CreateFrameResponse(byte value)
        {
            return new GetDesktopResponse
            {
                Image = new[] { value },
                Monitor = 1,
                Quality = 60,
                Resolution = new Resolution { Width = 1280, Height = 720 }
            };
        }

        private sealed class RecordingDispatcher : IServerCommandDispatcher
        {
            public List<CommandDispatchRequest> Requests { get; } = new List<CommandDispatchRequest>();

            public CommandDispatchResult NextResult { get; set; }

            public Task<CommandDispatchResult> DispatchAsync(
                string clientId,
                IMessage message,
                CancellationToken cancellationToken)
            {
                return DispatchAsync(CommandDispatchRequest.Create(clientId, message), cancellationToken);
            }

            public Task<CommandDispatchResult> DispatchAsync(
                CommandDispatchRequest request,
                CancellationToken cancellationToken)
            {
                Requests.Add(request);
                return Task.FromResult(NextResult ?? CommandDispatchResult.Sent(
                    request.CorrelationId,
                    DefaultCommandSafetyClassifier.Instance.Classify(request.Message)));
            }
        }

        private sealed class QueueResponseSource : IRemoteClientResponseSource
        {
            private readonly Queue<IMessage> _messages;

            public QueueResponseSource(params IMessage[] messages)
            {
                _messages = new Queue<IMessage>(messages);
            }

            public bool CancelCalled { get; private set; }

            public void CancelWait(string clientId)
            {
                CancelCalled = true;
            }

            public Task<IMessage> WaitForNextAsync(
                string clientId,
                TimeSpan timeout,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(_messages.Dequeue());
            }
        }
    }
}
