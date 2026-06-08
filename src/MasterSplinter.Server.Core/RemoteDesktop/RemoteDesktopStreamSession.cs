using MasterSplinter.Common.Messages;
using MasterSplinter.Server.Core.Commands;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public sealed class RemoteDesktopStreamSession
    {
        private readonly IServerCommandDispatcher _dispatcher;
        private readonly Func<GetDesktop, CancellationToken, Task<CommandDispatchRequest>> _requestFactory;
        private readonly IRemoteClientResponseSource _responses;

        public RemoteDesktopStreamSession(
            IServerCommandDispatcher dispatcher,
            IRemoteClientResponseSource responses,
            Func<GetDesktop, CancellationToken, Task<CommandDispatchRequest>> requestFactory)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _responses = responses ?? throw new ArgumentNullException(nameof(responses));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
        }

        public async Task<RemoteDesktopStreamResult> RunAsync(
            RemoteDesktopStreamOptions options,
            Func<RemoteDesktopStreamFrame, CancellationToken, Task> frameHandler,
            CancellationToken cancellationToken)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (frameHandler == null)
                throw new ArgumentNullException(nameof(frameHandler));

            int receivedFrames = 0;
            bool stoppedOnEmptyFrame = false;
            CommandDispatchResult lastDispatchResult = null;

            for (int frameNumber = 1; frameNumber <= options.FrameCount; frameNumber++)
            {
                var message = new GetDesktop
                {
                    CreateNew = frameNumber == 1,
                    Quality = options.Quality,
                    DisplayIndex = options.DisplayIndex
                };

                Task<IMessage> responseTask = _responses.WaitForNextAsync(
                    options.ClientId,
                    options.ResponseTimeout,
                    cancellationToken);
                CommandDispatchRequest request = await _requestFactory(message, cancellationToken)
                    .ConfigureAwait(false);

                lastDispatchResult = await _dispatcher.DispatchAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                if (lastDispatchResult.Status != CommandDispatchStatus.Sent)
                {
                    _responses.CancelWait(options.ClientId);
                    return RemoteDesktopStreamResult.Finished(
                        options.FrameCount,
                        receivedFrames,
                        false,
                        lastDispatchResult);
                }

                IMessage response;
                try
                {
                    response = await responseTask.ConfigureAwait(false);
                }
                catch
                {
                    _responses.CancelWait(options.ClientId);
                    throw;
                }

                while (!(response is GetDesktopResponse))
                {
                    response = await _responses.WaitForNextAsync(
                        options.ClientId,
                        options.ResponseTimeout,
                        cancellationToken).ConfigureAwait(false);
                }

                var desktopResponse = (GetDesktopResponse)response;

                receivedFrames++;
                await frameHandler(
                    new RemoteDesktopStreamFrame(frameNumber, desktopResponse),
                    cancellationToken).ConfigureAwait(false);

                if (desktopResponse.Image == null || desktopResponse.Image.Length == 0)
                {
                    stoppedOnEmptyFrame = true;
                    break;
                }

                if (options.FrameDelayMilliseconds > 0 && frameNumber < options.FrameCount)
                    await Task.Delay(options.FrameDelayMilliseconds, cancellationToken).ConfigureAwait(false);
            }

            return RemoteDesktopStreamResult.Finished(
                options.FrameCount,
                receivedFrames,
                stoppedOnEmptyFrame,
                lastDispatchResult);
        }
    }
}
