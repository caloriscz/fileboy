using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.App.ViewModels;

/// <summary>
/// Main ViewModel for the file browser window.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    private readonly INavigationHistory _navigationHistory;
    private readonly IProcessLauncher _processLauncher;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<MainViewModel> _logger;

    public MainViewModel(
        IFileSystemService fileSystemService,
        INavigationHistory navigationHistory,
        IProcessLauncher processLauncher,
        ISettingsService settingsService,
        ILogger<MainViewModel> logger)
    {
        _fileSystemService = fileSystemService;
        _navigationHistory = navigationHistory;
        _processLauncher = processLauncher;
        _settingsService = settingsService;
        _logger = logger;

        Items = [];
    }

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FileItemViewModel> _items;

    [ObservableProperty]
    private FileItemViewModel? _selectedItem;

    [ObservableProperty]
    private ViewMode _viewMode = ViewMode.List;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusText = "Ready";

    public bool CanGoBack => _navigationHistory.CanGoBack;
    public bool CanGoForward => _navigationHistory.CanGoForward;

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        ViewMode = _settingsService.Settings.DefaultView;
        
        var initialPath = _settingsService.Settings.LastPath;
        if (string.IsNullOrEmpty(initialPath) || !_fileSystemService.IsValidDirectory(initialPath))
        {
            initialPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }

        await NavigateToPathAsync(initialPath);
    }

    [RelayCommand]
    private async Task NavigateToPathAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        // Handle drive letters without backslash
        if (path.Length == 2 && path[1] == ':')
        {
            path += "\\";
        }

        if (!_fileSystemService.IsValidDirectory(path))
        {
            _logger.LogWarning("Invalid directory path: {Path}", path);
            StatusText = $"Invalid path: {path}";
            return;
        }

        IsLoading = true;
        StatusText = "Loading...";

        try
        {
            _logger.LogInformation("Navigating to: {Path}", path);
            
            var items = await _fileSystemService.GetItemsAsync(path);
            var sortedItems = items
                .OrderByDescending(i => i.IsDirectory)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(i => new FileItemViewModel(i))
                .ToList();

            Items = new ObservableCollection<FileItemViewModel>(sortedItems);
            CurrentPath = path;
            
            _navigationHistory.Navigate(path);
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            _settingsService.Settings.LastPath = path;
            await _settingsService.SaveAsync();

            StatusText = $"{Items.Count} items";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to {Path}", path);
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private async Task GoBackAsync()
    {
        var path = _navigationHistory.GoBack();
        if (!string.IsNullOrEmpty(path))
        {
            await LoadPathWithoutHistoryAsync(path);
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoForward))]
    private async Task GoForwardAsync()
    {
        var path = _navigationHistory.GoForward();
        if (!string.IsNullOrEmpty(path))
        {
            await LoadPathWithoutHistoryAsync(path);
        }
    }

    [RelayCommand]
    private async Task GoUpAsync()
    {
        var parent = Directory.GetParent(CurrentPath);
        if (parent != null)
        {
            await NavigateToPathAsync(parent.FullName);
        }
    }

    [RelayCommand]
    private async Task OpenSelectedItemAsync()
    {
        if (SelectedItem == null)
            return;

        await OpenItemAsync(SelectedItem);
    }

    public async Task OpenItemAsync(FileItemViewModel item)
    {
        _logger.LogInformation("Opening item: {Name}, IsDirectory: {IsDir}", item.Name, item.IsDirectory);

        if (item.IsDirectory)
        {
            await NavigateToPathAsync(item.FullPath);
        }
        else if (item.IsViewableImage)
        {
            // TODO: Open in built-in viewer
            // For now, open externally
            _processLauncher.OpenWithDefault(item.FullPath);
        }
        else
        {
            _processLauncher.OpenWithDefault(item.FullPath);
        }
    }

    [RelayCommand]
    private void SetViewMode(ViewMode mode)
    {
        ViewMode = mode;
        _settingsService.Settings.DefaultView = mode;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await NavigateToPathAsync(CurrentPath);
    }

    private async Task LoadPathWithoutHistoryAsync(string path)
    {
        IsLoading = true;
        StatusText = "Loading...";

        try
        {
            var items = await _fileSystemService.GetItemsAsync(path);
            var sortedItems = items
                .OrderByDescending(i => i.IsDirectory)
                .ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .Select(i => new FileItemViewModel(i))
                .ToList();

            Items = new ObservableCollection<FileItemViewModel>(sortedItems);
            CurrentPath = path;
            
            OnPropertyChanged(nameof(CanGoBack));
            OnPropertyChanged(nameof(CanGoForward));

            StatusText = $"{Items.Count} items";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {Path}", path);
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
