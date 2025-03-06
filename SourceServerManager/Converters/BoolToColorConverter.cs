using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace SourceServerManager.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isOnline)
        {
            return isOnline ? new SolidColorBrush(Colors.LimeGreen) : new SolidColorBrush(Colors.Red);
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}