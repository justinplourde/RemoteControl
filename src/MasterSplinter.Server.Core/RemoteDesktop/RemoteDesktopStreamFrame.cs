using MasterSplinter.Common.Messages;

namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public sealed class RemoteDesktopStreamFrame
    {
        public RemoteDesktopStreamFrame(int frameNumber, GetDesktopResponse response)
        {
            FrameNumber = frameNumber;
            Response = response;
        }

        public int FrameNumber { get; }

        public GetDesktopResponse Response { get; }
    }
}
