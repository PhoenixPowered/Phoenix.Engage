
namespace Phoenix.Security.Janrain
{
    public interface IWebBrowser
    {
        object BrowserDocument { get; }
        void Navigate(string uri);
        void NavigateToString(string html);
    }
}
