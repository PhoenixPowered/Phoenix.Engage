using System;
using Awesomium.Core;

namespace Phoenix.Engage
{
    public interface IAuthenticationWidget
    {
        String ApplicationName { get; }
        bool ForceReauth { get; }
        IWebView WebBrowser { get; }
        double Width { get; set; }
        double Height { get; set; }

    }
}
