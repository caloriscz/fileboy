using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileBoy.App.ViewModels;

namespace FileBoy.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
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

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}