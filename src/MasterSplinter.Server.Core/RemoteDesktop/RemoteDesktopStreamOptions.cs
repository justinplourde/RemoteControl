using System;

namespace MasterSplinter.Server.Core.RemoteDesktop
{
    public sealed class RemoteDesktopStreamOptions
    {
        public RemoteDesktopStreamOptions(
            string clientId,
            int quality,
            int displayIndex,
            int frameCount,
            int frameDelayMilliseconds,
            TimeSpan responseTimeout)
        {
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentException("Client id is required.", nameof(clientId));
            if (quality < 1 || quality > 100)
                throw new ArgumentOutOfRangeException(nameof(quality), "Quality must be between 1 and 100.");
            if (displayIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(displayIndex), "Display index must be zero or greater.");
            if (frameCount < 1)
                throw new ArgumentOutOfRangeException(nameof(frameCount), "Frame count must be one or greater.");
            if (frameDelayMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(frameDelayMilliseconds), "Frame delay must be zero or greater.");
            if (responseTimeout <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(responseTimeout), "Response timeout must be positive.");

            ClientId = clientId;
            Quality = quality;
            DisplayIndex = displayIndex;
            FrameCount = frameCount;
            FrameDelayMilliseconds = frameDelayMilliseconds;
            ResponseTimeout = responseTimeout;
        }

        public string ClientId { get; }

        public int Quality { get; }

        public int DisplayIndex { get; }

        public int FrameCount { get; }

        public int FrameDelayMilliseconds { get; }

        public TimeSpan ResponseTimeout { get; }
    }
}
