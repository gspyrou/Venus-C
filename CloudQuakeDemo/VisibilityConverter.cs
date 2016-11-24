using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;

namespace CloudQuakeDemo
{
    public class VisibilityConverter:IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var busy = (bool)value;
            if (parameter.ToString() == "Hide")
            {
                if (!busy)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            if (parameter.ToString() == "Show")
            {
                if (busy)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            if (parameter.ToString() == "Type")
            {
                if ((bool)value)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
