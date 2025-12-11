using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace FileBoy.App.Views;

/// <summary>
/// Dialog for saving edited images with file name input and overwrite warning.
/// </summary>
public partial class SaveImageDialog : Window
{
    private readonly string _originalPath;
    private readonly string _directory;

    public string FileName { get; private set; }
    public string FullPath { get; private set; }

    public SaveImageDialog(string originalPath, string? title = null)
    {
        InitializeComponent();
        
        _originalPath = originalPath;
        _directory = Path.GetDirectoryName(originalPath) ?? string.Empty;
        
        // Set custom title if provided
        if (!string.IsNullOrEmpty(title))
        {
            Title = title;
            TitleTextBlock.Text = title;
        }
        
        // Suggest a default name with numeric suffix
        var nameWithoutExt = Path.GetFileNameWithoutExtension(originalPath);
        var extension = Path.GetExtension(originalPath);
        var suggestedName = GenerateUniqueFileName(nameWithoutExt, extension);
        
        FileNameTextBox.Text = suggestedName;
        FileNameTextBox.SelectAll();
        FileNameTextBox.Focus();
        
        FileName = suggestedName;
        FullPath = Path.Combine(_directory, suggestedName);
        
        UpdateWarning();
    }

    private string GenerateUniqueFileName(string baseName, string extension)
    {
        var fileName = $"{baseName}{extension}";
        var fullPath = Path.Combine(_directory, fileName);
        
        // If original name doesn't exist, suggest it
        if (!File.Exists(fullPath))
        {
            return fileName;
        }
        
        // Try (2), (3), etc.
        var counter = 2;
        while (counter < 1000)
        {
            fileName = $"{baseName} ({counter}){extension}";
            fullPath = Path.Combine(_directory, fileName);
            
            if (!File.Exists(fullPath))
            {
                return fileName;
            }
            
            counter++;
        }
        
        // Fallback to timestamp if we somehow reach 1000 files
        return $"{baseName} ({DateTime.Now:yyyyMMdd_HHmmss}){extension}";
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
