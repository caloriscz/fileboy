using System.IO;
using System.Windows;

namespace FileBoy.App.Views;

/// <summary>
/// Interaction logic for RenameDialog.xaml
/// </summary>
public partial class RenameDialog : Window
{
    public string NewName { get; private set; } = string.Empty;
    private readonly bool _isDirectory;

    public RenameDialog(string currentName, bool isDirectory)
    {
        InitializeComponent();
        
        _isDirectory = isDirectory;
        CurrentNameTextBlock.Text = currentName;
        
        // Pre-fill with current name
        NewNameTextBox.Text = currentName;
        
        // Focus and select filename (without extension for files)
        Loaded += (s, e) =>
        {
            NewNameTextBox.Focus();
            
            if (!isDirectory && currentName.Contains('.'))
            {
                // Select just the filename part, not the extension
                var lastDotIndex = currentName.LastIndexOf('.');
                NewNameTextBox.Select(0, lastDotIndex);
            }
            else
            {
                // Select all for directories or files without extension
                NewNameTextBox.SelectAll();
            }
        };
    }

    private void RenameButton_Click(object sender, RoutedEventArgs e)
    {
        var newName = NewNameTextBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(newName))
        {
            MessageBox.Show(
                "Name cannot be empty.",
                "Invalid Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            NewNameTextBox.Focus();
            return;
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (newName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show(
                "Name contains invalid characters.",
                "Invalid Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            NewNameTextBox.Focus();
            return;
        }

        // Check if name is the same
        if (newName.Equals(CurrentNameTextBlock.Text, StringComparison.OrdinalIgnoreCase))
        {
            // No change, just close
            DialogResult = false;
            Close();
            return;
        }

        NewName = newName;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
