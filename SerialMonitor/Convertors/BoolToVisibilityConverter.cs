using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SerialMonitor.Convertors;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // Если есть параметр "Invert", инвертируем результат
            if (parameter is string param && param == "Invert")
                boolValue = !boolValue;

            return boolValue;
        }

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}