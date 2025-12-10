using System.Windows;
using FileBoy.App.ViewModels;

namespace FileBoy.App.Views;

/// <summary>
/// Dialog for copying or moving files to a selected folder.
/// </summary>
public partial class CopyMoveToFolderDialog : Window
{
    public CopyMoveToFolderViewModel ViewModel { get; }

    public CopyMoveToFolderDialog(CopyMoveToFolderViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.IsDestinationValid)
        {
            MessageBox.Show(
                "Please select a valid destination folder.",
                "Invalid Folder",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        ViewModel.SaveToHistory();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
