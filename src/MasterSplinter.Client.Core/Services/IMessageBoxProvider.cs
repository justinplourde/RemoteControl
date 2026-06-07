namespace MasterSplinter.Client.Core.Services
{
    public interface IMessageBoxProvider
    {
        void Show(string text, string caption, string button, string icon);
    }
}
