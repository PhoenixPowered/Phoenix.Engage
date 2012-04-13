using System.Windows;

namespace Phoenix.Windows.Engage
{
    public class TokenReceivedEventArgs : RoutedEventArgs
    {
        public string Token { get; set; }

        public TokenReceivedEventArgs(RoutedEvent id, string token)
        {
            RoutedEvent = id;
            Token = token;
        }
    }
}
