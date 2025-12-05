using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileBoy.App.Converters;

/// <summary>
/// Converts null to Visibility.Visible and non-null to Visibility.Collapsed.
/// Used to show fallback content when a value is null.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
    {
        // If value is null, show the element (fallback icon)
        // If value is not null (thumbnail exists), hide the element
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
