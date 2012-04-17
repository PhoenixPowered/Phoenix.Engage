using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Phoenix.Forms.Engage
{
    public class WidgetSizeChangedEventArgs : EventArgs
    {
        public int Height { get; set; }
        public int Width { get; set; }
        public PropertyChanged ChangedProperty { get; set; }

        public WidgetSizeChangedEventArgs(int height, int width, PropertyChanged propertyChanged)
        {
            Height = height;
            Width = width;
            ChangedProperty = propertyChanged;
        }

        public enum PropertyChanged
        {
            Width,
            Height
        }
    }
}
