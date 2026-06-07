using MasterSplinter.Server.Core.Commands;

namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public sealed class RemoteDesktopStreamResult
    {
        private RemoteDesktopStreamResult(
            int requestedFrames,
            int receivedFrames,
            bool stoppedOnEmptyFrame,
            CommandDispatchResult lastDispatchResult)
        {
            RequestedFrames = requestedFrames;
            ReceivedFrames = receivedFrames;
            StoppedOnEmptyFrame = stoppedOnEmptyFrame;
            LastDispatchResult = lastDispatchResult;
        }

        public int RequestedFrames { get; }

        public int ReceivedFrames { get; }

        public bool StoppedOnEmptyFrame { get; }

        public CommandDispatchResult LastDispatchResult { get; }

        public bool Completed => LastDispatchResult == null ||
            LastDispatchResult.Status == CommandDispatchStatus.Sent;

        public static RemoteDesktopStreamResult Finished(
            int requestedFrames,
            int receivedFrames,
            bool stoppedOnEmptyFrame,
            CommandDispatchResult lastDispatchResult)
        {
            return new RemoteDesktopStreamResult(
                requestedFrames,
                receivedFrames,
                stoppedOnEmptyFrame,
                lastDispatchResult);
        }
    }
}
