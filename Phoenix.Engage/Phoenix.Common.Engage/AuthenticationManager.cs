using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using Phoenix.Interop;
using SHDocVw;

namespace Phoenix.Security.Janrain
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AuthenticationManager : IDisposable
    {
        #region fields

        private const string LocalhostTokenUrl = "http://localhost/auth.html";
        public const string AccountSelectionUrl = "about:blank";

        private static readonly ProviderSpecCollection JanrainProviders = new ProviderSpecCollection();
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
        private readonly System.Timers.Timer _docCompleteTimer;

        private IWebBrowser2 _nativeBrowser;
        private DWebBrowserEvents_Event _browserEvents;
        private DWebBrowserEvents2_Event _browserEvents2;

        private string _widgetHtml;
        private IAuthenticationWidget _widget;
        private bool _isBusy;
        private double _originalWidth;
        private double _originalHeight;

        #endregion

        #region constructor

        /// <summary>
        /// 
        /// </summary>
        public AuthenticationManager()
        {
            SynchronizationContext context = SynchronizationContext.Current;
            _docCompleteTimer = new System.Timers.Timer(1500) {AutoReset = false};
            _docCompleteTimer.Elapsed += (sender, e) => PostAction(() => OnBusyStateChanged(false), context);

        }

        /// <summary>
        /// 
        /// </summary>
        static AuthenticationManager()
        {
            InitializeJanrainProviders();
        }

        #endregion

        #region events

        /// <summary>
        /// Event is fired when authentication is complete and the janrain token is received.
        /// </summary>
        public event EventHandler<TokenReceivedEventArgs> TokenReceived;
        /// <summary>
        /// Event is fired when the state of the underlying web browser has changed.  True  means that 
        /// the browser is currently navigating.
        /// </summary>
        public event EventHandler<BusyStateEventArgs> BusyStateChanged;

        #endregion

        #region public methods

        /// <summary>
        /// Initializes the authentication manager.
        /// </summary>
        /// <remarks>
        /// Once the authentication manager is initialized it takes control of the embedded 
        /// webbrowser of the widget.
        /// </remarks>
        /// <param name="widget"></param>
        public void Initialize(IAuthenticationWidget widget)
        {
            _widgetHtml = GetWidgetHtml(widget);
            widget.NavigateToString(_widgetHtml);
            _nativeBrowser = WebBrowserHelper.GetWebBrowser(widget.BrowserDocument);
            _widget = widget;
            _originalHeight = widget.Height;
            _originalWidth = widget.Width;

            ConnectEvents();
        }

        /// <summary>
        /// Navigates the embedded widget back to the account selection screen and resets widget size.
        /// </summary>
        public void SwitchAccounts()
        {
            NavigateBackToWidget();
        }

        public void Dispose()
        {
            if(_browserEvents != null && _browserEvents2 != null)
                DisconnectEvents();
            _nativeBrowser = null;

            if (_docCompleteTimer != null)
            {
                if(_docCompleteTimer.Enabled)
                    _docCompleteTimer.Stop();

                _docCompleteTimer.Dispose();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// Raise the BusyStateChanged event.
        /// </summary>
        /// <param name="isBusy"></param>
        private void OnBusyStateChanged(bool isBusy)
        {
            if (isBusy == _isBusy)
                return;

            _isBusy = isBusy;

            var handler = BusyStateChanged;
            if (handler != null)
                handler(this, new BusyStateEventArgs { IsBusy = isBusy });
        }

        /// <summary>
        /// Raises the TokenReceived event
        /// </summary>
        /// <param name="token"></param>
        private void OnTokenReceived(string token)
        {
            var handler = TokenReceived;
            if (handler != null)
                handler(this, new TokenReceivedEventArgs { Token = token });
        }

        /// <summary>
        /// Retrieves the base widget html from the embedded resource and builds
        /// rest of the html from the widget configuration.
        /// </summary>
        /// <param name="widget"></param>
        /// <returns></returns>
        private string GetWidgetHtml(IAuthenticationWidget widget)
        {
            string htmlFormatString = Properties.Resources.JanRainHtml;

            if (string.IsNullOrEmpty(htmlFormatString))
                throw new InvalidOperationException("html resource missing!");

            string forceReauth = widget.ForceReauth.ToString(CultureInfo.InvariantCulture).ToLower();

            return string.Format(htmlFormatString, widget.ApplicationName, LocalhostTokenUrl, forceReauth);
        }

        /// <summary>
        /// Posts the action to the UI Context asynchronously.
        /// </summary>
        /// <param name="toPost"></param>
        /// <param name="toPostTo"></param>
        private void PostAction(Action toPost, SynchronizationContext toPostTo = null)
        {
            SynchronizationContext context = toPostTo ?? _uiContext;

            if (context == null)
            {
                toPost();
                return;
            }

            context.Post(d => toPost(), null);
        }

        private void ConnectEvents()
        {
            _browserEvents = (DWebBrowserEvents_Event)_nativeBrowser;
            _browserEvents2 = (DWebBrowserEvents2_Event)_nativeBrowser;

            _browserEvents.BeforeNavigate += OnBeforeNavigate;
            _browserEvents2.DocumentComplete += OnDocumentComplete;
        }

        private void DisconnectEvents()
        {
            _browserEvents.BeforeNavigate -= OnBeforeNavigate;
            _browserEvents2.DocumentComplete -= OnDocumentComplete;

            _browserEvents = null;
            _browserEvents2 = null;
        }

        private void NavigateBackToWidget()
        {
            _widget.Width = _originalWidth;
            _widget.Height = _originalHeight;
            _widget.NavigateToString(_widgetHtml);
        }

        private void ProcessLoginComplete(byte[] postData)
        {
            // if we have no post data, then we can not be authenticated.
            if (postData == null || postData.Length == 0)
            {
                // go back to the selection screen.
                NavigateBackToWidget();
                return;
            }

            // if the last byte in the array represents a null byte skip it and get the string representation of the byte array.
            string postString = postData.Last() == 0 ? Encoding.Default.GetString(postData, 0, postData.Length - 1) : Encoding.Default.GetString(postData);

            // if we received an empty string from the post data, then we can't be logged in.
            if (string.IsNullOrEmpty(postString))
            {
                NavigateBackToWidget();
                return;
            }

            // remove token= from the string so we just have the token data.
            postString = postString.Replace("token=", string.Empty);

            DisconnectEvents();

            PostAction(() => OnTokenReceived(postString));
            PostAction(() => OnBusyStateChanged(false));

            _widget.Navigate(AccountSelectionUrl);
        }

        private void UpdateWidgetSize(string url)
        {
            // remove all url encoding
            string decodedUrl = Uri.UnescapeDataString(url);
            // find the beginning of the query string.
            int queryIdx = decodedUrl.IndexOf("?", StringComparison.InvariantCulture);
            // retrieve only the decoded query  string data
            decodedUrl = decodedUrl.Substring(queryIdx + 1);
            string provider = null;

            // split the query string @ the key/value separator
            var parts = decodedUrl.Split('&');
            // loop the found parts
            foreach (var part in parts)
            {
                // if the part is a provider name
                if (part.StartsWith("provider_name="))
                {
                    provider = part.Replace("provider_name=", "");
                    break;
                }
            }

            if (_originalHeight.Equals(default(double)))
                _originalHeight = _widget.Height;

            if (_originalWidth.Equals(default(double)))
                _originalWidth = _widget.Width;

            if (!string.IsNullOrEmpty(provider))
            {
                ProviderSpec spec;

                // lookup the provider in  our provider collection.
                if (JanrainProviders.TryGetValue(provider.ToLower(), out spec))
                {
                    _widget.Height = spec.Height + 45;
                    _widget.Width = spec.Width + 55;
                    return;
                }
            }

            // if we reached this point, we clearly didn't find a provider. 
            // set default dimensions
            _widget.Width = 800;
            _widget.Height = 600;
        }

        /// <summary>
        /// Initialize janrain provider details.
        /// </summary>
        /// <remarks>
        /// This information was scraped from janrains javascript.
        /// </remarks>
        private static void InitializeJanrainProviders()
        {
            JanrainProviders.Add(new ProviderSpec("aol", 514, 550));
            JanrainProviders.Add(new ProviderSpec("blogger", 800, 600));
            JanrainProviders.Add(new ProviderSpec("livejournal", 800, 600));
            JanrainProviders.Add(new ProviderSpec("netlog", 800, 600));
            JanrainProviders.Add(new ProviderSpec("wordpress", 800, 600));
            JanrainProviders.Add(new ProviderSpec("openid", 800, 600));
            JanrainProviders.Add(new ProviderSpec("flickr", 500, 500));
            JanrainProviders.Add(new ProviderSpec("google", 500, 450));
            JanrainProviders.Add(new ProviderSpec("hyves", 800, 600));
            JanrainProviders.Add(new ProviderSpec("myopenid", 800, 600));
            JanrainProviders.Add(new ProviderSpec("paypal", 800, 600));
            JanrainProviders.Add(new ProviderSpec("verisign", 945, 600));
            JanrainProviders.Add(new ProviderSpec("yahoo", 500, 550));
            JanrainProviders.Add(new ProviderSpec("facebook", 500, 500));
            JanrainProviders.Add(new ProviderSpec("myspace", 800, 500));
            JanrainProviders.Add(new ProviderSpec("twitter", 800, 500));
            JanrainProviders.Add(new ProviderSpec("linkedin", 550, 750));
            JanrainProviders.Add(new ProviderSpec("live_id", 900, 600));
            JanrainProviders.Add(new ProviderSpec("salesforce", 800, 500));
            JanrainProviders.Add(new ProviderSpec("orkut", 800, 600));
            JanrainProviders.Add(new ProviderSpec("vzn", 600, 450));
            JanrainProviders.Add(new ProviderSpec("foursquare", 950, 550));
            JanrainProviders.Add(new ProviderSpec("mixi", 950, 550));
        }

        #endregion

        #region event handlers

        void OnDocumentComplete(object automationObject, ref object url)
        {
            if (_docCompleteTimer.Enabled)
                _docCompleteTimer.Stop();

            _docCompleteTimer.Start();
        }

        void OnBeforeNavigate(string url, int flags, string targetFrameName, ref object postData, string headers, ref bool cancel)
        {
            _docCompleteTimer.Stop();
            PostAction(() => OnBusyStateChanged(true));

            if (url.StartsWith(LocalhostTokenUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                cancel = true;
                byte[] data = (byte[])postData;
                PostAction(() => ProcessLoginComplete(data));
            }

            if (url.Contains("rpxnow.com/") && url.Contains("/start"))
            {
                PostAction(() => UpdateWidgetSize(url));
            }
        }

        #endregion

        #region nested private types

        private class ProviderSpecCollection : Dictionary<string, ProviderSpec>
        {
            public void Add(ProviderSpec providerSpec)
            {
                if(providerSpec == null)
                    throw new ArgumentNullException("providerSpec");

                Add(providerSpec.Provider.ToLower(), providerSpec);
            }
        }

        private class ProviderSpec
        {
            public ProviderSpec(string provider, double width, double height)
            {
                Provider = provider;
                Height = height;
                Width = width;
            }

            public string Provider { get; private set; }
            public double Height { get; private set; }
            public double Width { get; private set; }
        }

        #endregion
    }
}
