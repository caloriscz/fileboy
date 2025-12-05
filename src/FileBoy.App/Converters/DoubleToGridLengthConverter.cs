using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FileBoy.App.Converters;

/// <summary>
/// Converts a double value to a GridLength and vice versa.
/// </summary>
public class DoubleToGridLengthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double width)
        {
            return new GridLength(width, GridUnitType.Pixel);
        }
        return new GridLength(400, GridUnitType.Pixel);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
        {
            return gridLength.Value;
        }
        return 400.0;
    }
}
