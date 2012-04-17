using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Phoenix.Engage;
using Awesomium.Core;

namespace Phoenix.Forms.Engage
{
    public partial class EngageWidget : UserControl, IAuthenticationWidget
    {
        #region fields

        private AuthenticationManager _authManager;
        private bool _isBusy;
        private int _widgetHeight;
        private int _widgetWidth;

        #endregion

        #region constructor

        public EngageWidget()
        {
            InitializeComponent();

#if DEBUG
            var config = WebCoreConfig.Default;
            config.LogLevel = LogLevel.Verbose;
            config.LogPath = Environment.CurrentDirectory;
            WebCore.Initialize(config, false);
#endif
        } 

        #endregion

        #region events

        public event EventHandler<TokenReceivedEventArgs> TokenReceived;
        public event EventHandler<BusyStateEventArgs> BusyStateChanged;
        public event EventHandler<WidgetSizeChangedEventArgs> WidgetSizeChanged;

        #endregion

        #region control properties

        public string ApplicationName { get; set; }
        public bool ForceReauth { get; set; }
        public bool CanSwitchAccounts { get; set; }

        #endregion

        #region control methods

        public void SwitchAccounts()
        {
            if(_authManager == null || !CanSwitchAccounts)
                return;

            _authManager.SwitchAccounts();
        }

        #endregion

        #region protected methods

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            SetupResources();
        }

        protected void OnWidgetSizeChanged(WidgetSizeChangedEventArgs.PropertyChanged whatChanged)
        {
            var handler = WidgetSizeChanged;
            if(handler != null)
                handler(this, new WidgetSizeChangedEventArgs(_widgetHeight, _widgetWidth, whatChanged));
        }

        protected void OnTokenReceived(string token)
        {
            if (string.IsNullOrEmpty(token))
                return;

            var handler = TokenReceived;
            if (handler != null)
                handler(this, new TokenReceivedEventArgs { Token = token });
        }

        protected void OnBusyStateChanged(bool isBusy)
        {
            if (isBusy == _isBusy)
                return;

            _isBusy = isBusy;

            var handler = BusyStateChanged;
            if (handler != null)
                handler(this, new BusyStateEventArgs { IsBusy = isBusy });
        }

        protected void SetupResources()
        {
            _widgetHeight = this.Size.Height;
            _widgetWidth = this.Size.Width;

            AuthBrowser.BeginNavigation += AuthBrowserOnBeginNavigation;

            _authManager = new AuthenticationManager(Resources.EngageHtml);
            _authManager.TokenReceived += AuthManagerOnTokenReceived;
            _authManager.BusyStateChanged += AuthManagerOnBusyStateChanged;
            _authManager.Initialize(this);
        }

        protected void ReleaseResources()
        {
            if (_authManager != null)
            {
                _authManager.BusyStateChanged -= AuthManagerOnBusyStateChanged;
                _authManager.TokenReceived -= AuthManagerOnTokenReceived;
                _authManager.Dispose();
                _authManager = null;
            }

            if (AuthBrowser != null)
            {
                AuthBrowser.BeginNavigation -= AuthBrowserOnBeginNavigation;
            }
        }

        #endregion

        #region explicit interface implemenations

        IWebView IAuthenticationWidget.WebBrowser
        {
            get { return AuthBrowser; }
        }

        double IAuthenticationWidget.Height
        {
            get { return _widgetHeight; }
            set
            {
                int intValue = (int) value;
                if(intValue == _widgetHeight)
                    return;

                _widgetHeight = intValue;
                OnWidgetSizeChanged(WidgetSizeChangedEventArgs.PropertyChanged.Height);
            }
        }

        double IAuthenticationWidget.Width
        {
            get { return _widgetWidth; }
            set
            {
                int intValue = (int) value;
                if(intValue == _widgetWidth)
                    return;

                _widgetWidth = intValue;
                OnWidgetSizeChanged(WidgetSizeChangedEventArgs.PropertyChanged.Width);
            }
        }

        #endregion

        #region event handlers

        private void AuthManagerOnBusyStateChanged(object sender, BusyStateEventArgs e)
        {
            OnBusyStateChanged(e.IsBusy);
        }

        void AuthManagerOnTokenReceived(object sender, TokenReceivedEventArgs e)
        {
            OnTokenReceived(e.Token);
        }

        private void AuthBrowserOnBeginNavigation(object sender, BeginNavigationEventArgs beginNavigationEventArgs)
        {
            if(_authManager == null)
                return;

            string url = beginNavigationEventArgs.Url;
            CanSwitchAccounts = url.StartsWith(AuthenticationManager.LocalhostWidgetUrl) == false;
        }

        #endregion
    }
}
