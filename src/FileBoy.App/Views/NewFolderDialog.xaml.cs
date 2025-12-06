using System.IO;
using System.Windows;

namespace FileBoy.App.Views;

/// <summary>
/// Interaction logic for NewFolderDialog.xaml
/// </summary>
public partial class NewFolderDialog : Window
{
    public string FolderName { get; private set; } = string.Empty;

    public NewFolderDialog()
    {
        InitializeComponent();
        
        // Set default folder name
        FolderName = "New Folder";
        FolderNameTextBox.Text = FolderName;
        
        // Focus and select all text when loaded
        Loaded += (s, e) =>
        {
            FolderNameTextBox.Focus();
            FolderNameTextBox.SelectAll();
        };
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var folderName = FolderNameTextBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(folderName))
        {
            MessageBox.Show(
                "Folder name cannot be empty.",
                "Invalid Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            FolderNameTextBox.Focus();
            return;
        }

        // Check for invalid characters
        var invalidChars = Path.GetInvalidFileNameChars();
        if (folderName.IndexOfAny(invalidChars) >= 0)
        {
            MessageBox.Show(
                "Folder name contains invalid characters.",
                "Invalid Name",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            FolderNameTextBox.Focus();
            return;
        }

        FolderName = folderName;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
