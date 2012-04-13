using System;

namespace Phoenix.Security.Janrain
{
    public class TokenReceivedEventArgs : EventArgs
    {
        public string Token { get; set; }
    }
}
