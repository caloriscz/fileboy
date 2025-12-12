using System.Windows;
using FileBoy.App.ViewModels;
using Microsoft.Win32;

namespace FileBoy.App.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void BrowseSnapshotFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Snapshot Folder",
            InitialDirectory = ((SettingsViewModel)DataContext).SnapshotFolder
        };

        if (dialog.ShowDialog() == true)
        {
            ((SettingsViewModel)DataContext).SnapshotFolder = dialog.FolderName;
        }
    }
}
