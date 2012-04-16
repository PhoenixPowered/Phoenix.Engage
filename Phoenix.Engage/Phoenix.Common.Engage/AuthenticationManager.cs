using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using Awesomium.Core;

namespace Phoenix.Engage
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class AuthenticationManager : IDisposable
    {
        #region fields

        internal const string LocalhostWidgetUrl = "http://localhost/auth.html";
        internal readonly string LocalhostTokenUrl;

        private static readonly ProviderSpecCollection JanrainProviders = new ProviderSpecCollection();
        private readonly SynchronizationContext _uiContext = SynchronizationContext.Current;
        private readonly System.Timers.Timer _docCompleteTimer;
        private readonly string _baseWidgetHtml;

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
        public AuthenticationManager(string baseWidgetHtml)
        {
            SynchronizationContext context = SynchronizationContext.Current;
            _docCompleteTimer = new System.Timers.Timer(500) {AutoReset = false};
            _docCompleteTimer.Elapsed += (sender, e) => PostAction(() => OnBusyStateChanged(false), context);
            _baseWidgetHtml = baseWidgetHtml;
            LocalhostTokenUrl = LocalhostWidgetUrl + "?token";
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
            _widget = widget;
            _originalHeight = widget.Height;
            _originalWidth = widget.Width;

            ConnectEvents();
            _widget.WebBrowser.LoadURL(LocalhostWidgetUrl);
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
            if(_widget != null && _widget.WebBrowser != null)
                DisconnectEvents();
            _widget = null;

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
            string htmlFormatString = _baseWidgetHtml;

            if (string.IsNullOrEmpty(htmlFormatString))
                throw new InvalidOperationException("html resource missing!");

            string forceReauth = widget.ForceReauth.ToString(CultureInfo.InvariantCulture).ToLower();

            return string.Format(htmlFormatString, widget.ApplicationName, LocalhostWidgetUrl, forceReauth);
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
            _widget.WebBrowser.ResourceRequest += WebBrowserOnResourceRequest;
            _widget.WebBrowser.LoadCompleted += WebBrowserOnLoadCompleted;
            _widget.WebBrowser.BeginNavigation += WebBrowserOnBeginNavigation;
        }

        private void DisconnectEvents()
        {
            _widget.WebBrowser.ResourceRequest -= WebBrowserOnResourceRequest;
            _widget.WebBrowser.LoadCompleted -= WebBrowserOnLoadCompleted;
            _widget.WebBrowser.BeginNavigation -= WebBrowserOnBeginNavigation;
        }

        private void NavigateBackToWidget()
        {
            _widget.Width = _originalWidth;
            _widget.Height = _originalHeight;
            _widget.WebBrowser.LoadURL(LocalhostWidgetUrl);
        }

        private void ProcessLoginComplete(string postData)
        {
            // if we received an empty string from the post data, then we can't be logged in.
            if (string.IsNullOrEmpty(postData))
            {
                NavigateBackToWidget();
                return;
            }

            // remove token= from the string so we just have the token data.
            postData = postData.Replace("token=", string.Empty);

            PostAction(() => OnTokenReceived(postData));
            PostAction(() => OnBusyStateChanged(false));

            NavigateBackToWidget();
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

        private void WebBrowserOnBeginNavigation(object sender, BeginNavigationEventArgs beginNavigationEventArgs)
        {
            ProcessBeginNavigation(beginNavigationEventArgs.Url);
        }

        private void WebBrowserOnLoadCompleted(object sender, EventArgs eventArgs)
        {
            if (_docCompleteTimer.Enabled)
                _docCompleteTimer.Stop();

            _docCompleteTimer.Start();
        }

        private ResourceResponse WebBrowserOnResourceRequest(object sender, ResourceRequestEventArgs resourceRequestEventArgs)
        {
            string url = resourceRequestEventArgs.Request.Url;

            if (url.StartsWith(LocalhostWidgetUrl, StringComparison.InvariantCultureIgnoreCase))
            {
                if (resourceRequestEventArgs.Request.Method.Equals("post", StringComparison.InvariantCultureIgnoreCase)
                    && resourceRequestEventArgs.Request.UploadElementsCount > 0)
                {
                    if (resourceRequestEventArgs.Request.UploadElementsCount < 1)
                        throw new InvalidOperationException();

                    var uploadElement = resourceRequestEventArgs.Request.GetUploadElement(0);
                    string data = uploadElement.GetBytes();
                    PostAction(() => ProcessLoginComplete(data));
                }

                var widgetBytes = Encoding.UTF8.GetBytes(_widgetHtml);
                var response = new ResourceResponse(widgetBytes, "text/html");

                return response;
            }

            return null;
        }

        private void ProcessBeginNavigation(string url)
        {
            _docCompleteTimer.Stop();
            PostAction(() => OnBusyStateChanged(true));

            if (url.Contains("rpxnow.com/") && url.Contains("/start"))
            {
                PostAction(() => UpdateWidgetSize(url));
            }

            if (url.Equals(LocalhostWidgetUrl) && !_widget.Height.Equals(_originalHeight))
            {
                _widget.Height = _originalHeight;
                _widget.Width = _originalWidth;
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
