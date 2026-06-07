using MasterSplinter.Common.Messages;

namespace MasterSplinter.Client.Core.RemoteDesktop
{
    public interface IRemoteInputProvider
    {
        RemoteInputResult SendMouseEvent(DoMouseEvent mouseEvent);

        RemoteInputResult SendKeyboardEvent(DoKeyboardEvent keyboardEvent);
    }
}
