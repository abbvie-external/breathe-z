using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace BreatheZ.Ui.Util
{
    public class VisibilityOnNonZeroConverter : IValueConverter
    {
        public Visibility OnZero { get; set; }
        public Visibility OnNonZero { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is int && ((int)value) != 0 ? OnNonZero : OnZero;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}