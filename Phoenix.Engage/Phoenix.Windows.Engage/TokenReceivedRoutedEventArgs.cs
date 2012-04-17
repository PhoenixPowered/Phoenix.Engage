using System.Windows;

namespace Phoenix.Windows.Engage
{
    public class TokenReceivedRoutedEventArgs : RoutedEventArgs
    {
        public string Token { get; set; }

        public TokenReceivedRoutedEventArgs(RoutedEvent id, string token)
        {
            RoutedEvent = id;
            Token = token;
        }
    }
}
