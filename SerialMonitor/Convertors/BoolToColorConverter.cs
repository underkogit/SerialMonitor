using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SerialMonitor.Convertors;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isConnected)
        {
            return isConnected ? Brushes.Green : Brushes.Gray;
        }

        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}