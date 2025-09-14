using Avalonia.Data.Converters;
using System.Globalization;
using System;

namespace SourceServerManager.Converters;

public class EnumToIntConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum)
            return (int)value;
        return 0;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int intValue && targetType.IsEnum)
            return Enum.ToObject(targetType, intValue);
        return Enum.GetValues(targetType).GetValue(0) ?? 0;
    }
}