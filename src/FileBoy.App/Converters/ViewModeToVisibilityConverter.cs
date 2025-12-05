using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FileBoy.Core.Enums;

namespace FileBoy.App.Converters;

/// <summary>
/// Converts ViewMode enum to Visibility or boolean for binding.
/// </summary>
public class ViewModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not ViewMode currentMode || parameter is not string targetModeStr)
            return Visibility.Collapsed;

        var targetMode = Enum.Parse<ViewMode>(targetModeStr);
        
        if (targetType == typeof(Visibility))
        {
            return currentMode == targetMode ? Visibility.Visible : Visibility.Collapsed;
        }
        
        // For ToggleButton IsChecked binding
        return currentMode == targetMode;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is string modeStr && value is true)
        {
            return Enum.Parse<ViewMode>(modeStr);
        }
        return Binding.DoNothing;
    }
}
