using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileBoy.App.ViewModels;

namespace FileBoy.App.Pages;

/// <summary>
/// Interaction logic for BrowserPage.xaml
/// </summary>
public partial class BrowserPage : Page
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public BrowserPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Ensure page can receive keyboard input
        Focusable = true;
        Loaded += (s, e) => Focus();
    }

    private void PathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = (TextBox)sender;
            ViewModel.NavigateToPathCommand.Execute(textBox.Text);
            e.Handled = true;
        }
    }

    private async void FileListGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedItem != null)
        {
            await ViewModel.OpenItemAsync(ViewModel.SelectedItem);
        }
    }

    private async void ThumbnailList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedItem != null)
        {
            await ViewModel.OpenItemAsync(ViewModel.SelectedItem);
        }
    }
}
