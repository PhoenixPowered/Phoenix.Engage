using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Awesomium.Core;
using Awesomium.Windows.Controls;
using Phoenix.Engage;

namespace Phoenix.Windows.Engage
{
	/// <summary>
	/// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
	///
	/// Step 1a) Using this custom control in a XAML file that exists in the current project.
	/// Add this XmlNamespace attribute to the root element of the markup file where it is 
	/// to be used:
	///
	///     xmlns:MyNamespace="clr-namespace:Phoenix.Windows.Engage"
	///
	///
	/// Step 1b) Using this custom control in a XAML file that exists in a different project.
	/// Add this XmlNamespace attribute to the root element of the markup file where it is 
	/// to be used:
	///
	///     xmlns:MyNamespace="clr-namespace:Phoenix.Windows.Engage;assembly=Phoenix.Windows.Engage"
	///
	/// You will also need to add a project reference from the project where the XAML file lives
	/// to this project and Rebuild to avoid compilation errors:
	///
	///     Right click on the target project in the Solution Explorer and
	///     "Add Reference"->"Projects"->[Browse to and select this project]
	///
	///
	/// Step 2)
	/// Go ahead and use your control in the XAML file.
	///
	///     <MyNamespace:EngageWidget/>
	///
	/// </summary>
	public class EngageWidget : Control, IAuthenticationWidget
	{
		#region fields

		private const string AuthBrowserName = "AuthBrowser";

		private WebControl _webBrowser;
		private AuthenticationManager _authenticationManager; 

		#endregion

		#region constructors
		
		static EngageWidget()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(EngageWidget), new FrameworkPropertyMetadata(typeof(EngageWidget)));

#if DEBUG
			var config = WebCoreConfig.Default;
			config.LogLevel = LogLevel.Verbose;
			config.LogPath = Environment.CurrentDirectory;
			WebCore.Initialize(config, false);
#endif
		}

		#endregion

		#region control events

		#region TokenReceived Routed Event

		public static readonly RoutedEvent TokenReceivedEvent = EventManager.RegisterRoutedEvent(
			"TokenReceived",
			RoutingStrategy.Bubble,
			typeof(EventHandler<TokenReceivedRoutedEventArgs>),
			typeof(EngageWidget));

		public event EventHandler<TokenReceivedRoutedEventArgs> TokenReceived
		{
			add { AddHandler(TokenReceivedEvent, value); }
			remove { RemoveHandler(TokenReceivedEvent, value); }
		}

		protected virtual void OnTokenReceived(string token)
		{
			TokenValue = token;
			RaiseEvent(new TokenReceivedRoutedEventArgs(TokenReceivedEvent, token));
		}

		#endregion

		#endregion

		#region control properties

		#region ApplicationName Dependency Property

		public static readonly DependencyProperty ApplicationNameProperty =
			DependencyProperty.Register("ApplicationName", typeof(string), typeof(EngageWidget), new PropertyMetadata(default(string)));

		public string ApplicationName
		{
			get { return (string)GetValue(ApplicationNameProperty); }
			set { SetValue(ApplicationNameProperty, value); }
		}

		#endregion

		#region IsBusy Dependency Property

		public static readonly DependencyProperty IsBusyProperty =
			DependencyProperty.Register("IsBusy", typeof(bool), typeof(EngageWidget),
										new PropertyMetadata(default(bool)));
		public bool IsBusy
		{
			get { return (bool)GetValue(IsBusyProperty); }
			set { SetValue(IsBusyProperty, value); }
		}

		#endregion

		#region TokenValue Dependency Property

		public static readonly DependencyProperty TokenValueProperty =
			DependencyProperty.Register("TokenValue", typeof(string), typeof(EngageWidget), new PropertyMetadata(default(string)));

		public string TokenValue
		{
			get { return (string)GetValue(TokenValueProperty); }
			set { SetValue(TokenValueProperty, value); }
		}

		#endregion

		#region ForceReauth Dependency Property

		public static readonly DependencyProperty ForceReauthProperty =
			DependencyProperty.Register("ForceReauth", typeof(bool), typeof(EngageWidget), new PropertyMetadata(default(bool)));

		public bool ForceReauth
		{
			get { return (bool)GetValue(ForceReauthProperty); }
			set { SetValue(ForceReauthProperty, value); }
		}

		#endregion

		#region SwitchAccounts Dependency Property

		public static readonly DependencyProperty SwitchAccountsProperty =
			DependencyProperty.Register("SwitchAccounts", typeof(ICommand), typeof(EngageWidget), new PropertyMetadata(default(ICommand)));

		public ICommand SwitchAccounts
		{
			get { return (ICommand)GetValue(SwitchAccountsProperty); }
			set { SetValue(SwitchAccountsProperty, value); }
		}

		#endregion

		IWebView IAuthenticationWidget.WebBrowser
		{
			get { return _webBrowser; }
		}

		#endregion

		#region methods
		
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			DetachFromVisualTree();
			AttachToVisualTree();
		}

		private void AttachToVisualTree()
		{
			_webBrowser = GetTemplateChild(AuthBrowserName) as WebControl;
			if (_webBrowser == null)
				throw new InvalidOperationException("WebControl not found!");

			_webBrowser.BeginNavigation += WebBrowserOnBeginNavigation;

			_authenticationManager = new AuthenticationManager(Properties.Resources.EngageHtml);
			_authenticationManager.TokenReceived += AuthenticationManagerOnTokenReceived;
			_authenticationManager.BusyStateChanged += AuthenticationManagerOnBusyStateChanged;
			_authenticationManager.Initialize(this);

			SwitchAccounts = new SwitchAccountsCommand(_authenticationManager);
		}

		private void DetachFromVisualTree()
		{
			if (_authenticationManager != null)
			{
				_authenticationManager.TokenReceived -= AuthenticationManagerOnTokenReceived;
				_authenticationManager.BusyStateChanged -= AuthenticationManagerOnBusyStateChanged;
				_authenticationManager.Dispose();
				_authenticationManager = null;
			}

			if (_webBrowser != null)
			{
				_webBrowser.BeginNavigation -= WebBrowserOnBeginNavigation;
				_webBrowser = null;
			}
		}

		#endregion

		#region event handlers
		
		private void WebBrowserOnBeginNavigation(object sender, BeginNavigationEventArgs beginNavigationEventArgs)
		{
			if (SwitchAccounts == null)
				return;

			string url = beginNavigationEventArgs.Url;
			var switchAccounts = (SwitchAccountsCommand)SwitchAccounts;
			switchAccounts.IsEnabled = url.StartsWith(AuthenticationManager.LocalhostWidgetUrl) == false;
		}

		private void AuthenticationManagerOnBusyStateChanged(object sender, BusyStateEventArgs busyStateEventArgs)
		{
			if(IsBusy == busyStateEventArgs.IsBusy)
				return;
			IsBusy = busyStateEventArgs.IsBusy;
		}

		private void AuthenticationManagerOnTokenReceived(object sender, Phoenix.Engage.TokenReceivedEventArgs tokenReceivedEventArgs)
		{
			OnTokenReceived(tokenReceivedEventArgs.Token);
		}

		#endregion
	}
}
