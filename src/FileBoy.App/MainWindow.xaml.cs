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

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.RefreshCommand.Execute(null);
    }

    private void ListView_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.SetViewModeCommand.Execute("List");
    }

    private void ThumbnailView_Click(object sender, RoutedEventArgs e)
    {
        _mainViewModel.SetViewModeCommand.Execute("Thumbnail");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}