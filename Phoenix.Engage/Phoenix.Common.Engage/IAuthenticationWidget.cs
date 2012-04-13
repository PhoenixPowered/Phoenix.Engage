using System;

namespace Phoenix.Security.Janrain
{
    public interface IAuthenticationWidget : IWebBrowser
    {
        String ApplicationName { get; set; }
        bool ForceReauth { get; set; }
        double Width { get; set; }
        double Height { get; set; }
    }
}
