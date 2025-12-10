using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FileBoy.App.Views;

/// <summary>
/// Dialog for saving cropped images with file name input and overwrite warning.
/// </summary>
public partial class SaveImageDialog : Window
{
    private readonly string _originalPath;
    private readonly string _directory;

    public string FileName { get; private set; }
    public string FullPath { get; private set; }

    public SaveImageDialog(string originalPath)
    {
        InitializeComponent();
        
        _originalPath = originalPath;
        _directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        
        // Suggest a default name based on original
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);
        var suggestedName = $"{nameWithoutExt}_cropped{extension}";
        
        FileNameTextBox.Text = suggestedName;
        FileNameTextBox.SelectAll();
        FileNameTextBox.Focus();
        
        FileName = suggestedName;
        FullPath = Path.Combine(_directory, suggestedName);
        
        UpdateWarning();
    }

    private void FileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        FileName = FileNameTextBox.Text;
        
        if (!string.IsNullOrWhiteSpace(FileName))
        {
            FullPath = Path.Combine(_directory, FileName);
            UpdateWarning();
        }
    }

    private void UpdateWarning()
    {
        if (File.Exists(FullPath))
        {
            WarningPanel.Visibility = Visibility.Visible;
            
            if (string.Equals(FullPath, _originalPath, StringComparison.OrdinalIgnoreCase))
            {
                WarningText.Text = "The original image will be replaced.";
            }
            else
            {
                WarningText.Text = "A file with this name already exists. It will be replaced.";
            }
        }
        else
        {
            WarningPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FileName))
        {
            MessageBox.Show(
                "Please enter a file name.",
                "Invalid File Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Validate file name
        var invalidChars = Path.GetInvalidFileNameChars();
        if (FileName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show(
                "The file name contains invalid characters.",
                "Invalid File Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
