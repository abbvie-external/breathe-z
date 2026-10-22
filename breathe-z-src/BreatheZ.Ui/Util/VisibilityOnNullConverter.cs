using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace BreatheZ.Ui.Util
{
    public class VisibilityOnNullConverter : IValueConverter
    {
        public Visibility OnNull { get; set; }
        public Visibility OnNonNull { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? OnNull : OnNonNull;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}