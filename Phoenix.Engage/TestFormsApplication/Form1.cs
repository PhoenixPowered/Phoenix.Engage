using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Phoenix.Engage;
using Phoenix.Forms.Engage;

namespace TestFormsApplication
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            engageWidget1.WidgetSizeChanged += EngageWidget1OnWidgetSizeChanged;
            engageWidget1.TokenReceived += EngageWidget1OnTokenReceived;
        }

        private void EngageWidget1OnTokenReceived(object sender, TokenReceivedEventArgs tokenReceivedEventArgs)
        {
            MessageBox.Show(tokenReceivedEventArgs.Token);
        }

        private void EngageWidget1OnWidgetSizeChanged(object sender, WidgetSizeChangedEventArgs widgetSizeChangedEventArgs)
        {
            if (widgetSizeChangedEventArgs.ChangedProperty == WidgetSizeChangedEventArgs.PropertyChanged.Width)
                Width = widgetSizeChangedEventArgs.Width;

            if (widgetSizeChangedEventArgs.ChangedProperty == WidgetSizeChangedEventArgs.PropertyChanged.Height)
                Height = widgetSizeChangedEventArgs.Height + 35;
        }
    }
}
