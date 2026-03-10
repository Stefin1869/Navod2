using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Navod2.App;

/// <summary>null → Collapsed, non-null → Visible</summary>
[ValueConversion(typeof(object), typeof(Visibility))]
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>bool → inverted bool (true → false, false → true)</summary>
[ValueConversion(typeof(bool), typeof(bool))]
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is false or null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is false or null;
}

/// <summary>bool → Green/Red brush (indikátor dostupnosti LanguageTool)</summary>
[ValueConversion(typeof(bool), typeof(Brush))]
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Brushes.Green : Brushes.Red;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
