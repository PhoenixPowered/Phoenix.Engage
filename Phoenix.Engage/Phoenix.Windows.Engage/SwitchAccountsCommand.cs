using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Phoenix.Engage;

namespace Phoenix.Windows.Engage
{
    internal class SwitchAccountsCommand : ICommand
    {
        private bool _isEnabled;
        private readonly AuthenticationManager _authManager;

        public SwitchAccountsCommand(AuthenticationManager authManager)
        {
            _authManager = authManager;
        }

        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (value != _isEnabled)
                {
                    _isEnabled = value;
                    var handler = CanExecuteChanged;
                    if (handler != null)
                        handler(this, EventArgs.Empty);
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            return IsEnabled;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _authManager.SwitchAccounts();
        }
    }
}
