using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace SourceServerManager.Converters;

public class BoolToStatusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline ? "Online" : "Offline";
        }

        return "Unknown";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}