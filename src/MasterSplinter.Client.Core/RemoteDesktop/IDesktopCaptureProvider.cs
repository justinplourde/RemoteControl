using MasterSplinter.Common.Messages;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public interface IDesktopCaptureProvider
    {
        GetDesktopResponse Capture(GetDesktop request);
    }
}
