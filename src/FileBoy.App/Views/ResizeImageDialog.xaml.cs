using System.Windows;
using System.Windows.Controls;

namespace FileBoy.App.Views;

/// <summary>
/// Dialog for resizing images with width, height inputs and keep aspect ratio option.
/// </summary>
public partial class ResizeImageDialog : Window
{
    private readonly int _originalWidth;
    private readonly int _originalHeight;
    private readonly double _aspectRatio;
    private bool _isUpdating;

    public int NewWidth { get; private set; }
    public int NewHeight { get; private set; }
    public bool KeepAspectRatio => KeepAspectRatioCheckBox.IsChecked == true;

    public ResizeImageDialog(int originalWidth, int originalHeight)
    {
        InitializeComponent();
        
        _originalWidth = originalWidth;
        _originalHeight = originalHeight;
        _aspectRatio = (double)originalWidth / originalHeight;
        
        OriginalSizeText.Text = $"Original size: {originalWidth} × {originalHeight} px";
        
        WidthTextBox.Text = originalWidth.ToString();
        HeightTextBox.Text = originalHeight.ToString();
        
        NewWidth = originalWidth;
        NewHeight = originalHeight;
        
        WidthTextBox.SelectAll();
        WidthTextBox.Focus();
    }

    private void WidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        
        if (int.TryParse(WidthTextBox.Text, out var width) && width > 0)
        {
            if (KeepAspectRatio)
            {
                _isUpdating = true;
                NewWidth = width;
                NewHeight = (int)Math.Round(width / _aspectRatio);
                HeightTextBox.Text = NewHeight.ToString();
                _isUpdating = false;
            }
            else
            {
                NewWidth = width;
            }
        }
    }

    private void HeightTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating) return;
        
        if (int.TryParse(HeightTextBox.Text, out var height) && height > 0)
        {
            if (KeepAspectRatio)
            {
                _isUpdating = true;
                NewWidth = (int)Math.Round(height * _aspectRatio);
                NewHeight = height;
                WidthTextBox.Text = NewWidth.ToString();
                _isUpdating = false;
            }
            else
            {
                NewHeight = height;
            }
        }
    }

    private void KeepAspectRatioCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (KeepAspectRatio && int.TryParse(WidthTextBox.Text, out var width) && width > 0)
        {
            _isUpdating = true;
            NewHeight = (int)Math.Round(width / _aspectRatio);
            HeightTextBox.Text = NewHeight.ToString();
            _isUpdating = false;
        }
    }

    private void ResizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(WidthTextBox.Text, out var width) || width <= 0)
        {
            MessageBox.Show(
                "Please enter a valid width (positive integer).",
                "Invalid Width",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(HeightTextBox.Text, out var height) || height <= 0)
        {
            MessageBox.Show(
                "Please enter a valid height (positive integer).",
                "Invalid Height",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        NewWidth = width;
        NewHeight = height;
        
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
