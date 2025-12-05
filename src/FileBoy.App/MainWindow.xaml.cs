using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FileBoy.App.Pages;
using FileBoy.App.Services;
using FileBoy.App.ViewModels;

namespace FileBoy.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _mainViewModel;
    private readonly PageNavigationService _navigationService;

    public MainWindow(MainViewModel mainViewModel, IPageNavigationService navigationService)
    {
        InitializeComponent();
        
        _mainViewModel = mainViewModel;
        _navigationService = (PageNavigationService)navigationService;
        _navigationService.SetFrame(MainFrame);
        
        // Bind menu items to ViewModel commands
        RefreshMenuItem.Command = _mainViewModel.RefreshCommand;
        ListViewMenuItem.Command = _mainViewModel.SetViewModeCommand;
        ListViewMenuItem.CommandParameter = "List";
        ThumbnailViewMenuItem.Command = _mainViewModel.SetViewModeCommand;
        ThumbnailViewMenuItem.CommandParameter = "Thumbnail";
        SettingsMenuItem.Command = _mainViewModel.OpenSettingsCommand;
        
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to browser page
        var browserPage = new BrowserPage(_mainViewModel);
        MainFrame.Navigate(browserPage);
        
        // Initialize the view model
        await _mainViewModel.InitializeAsync();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "FileBoy - Media File Manager\n\nVersion 1.0.0\n\nA lightweight file browser with image preview and thumbnail support.",
            "About FileBoy",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}