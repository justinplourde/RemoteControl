namespace MasterSplinter.Client.Core.Services
{
    public interface IWebsiteVisitProvider
    {
        void Visit(string url, bool hidden);
    }
}
