using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace pixeledit2.Converters;

public class EnumToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.Equals(parameter);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.Equals(true) == true ? parameter : AvaloniaProperty.UnsetValue;
    }
}
