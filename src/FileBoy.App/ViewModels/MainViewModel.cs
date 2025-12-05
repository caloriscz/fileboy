using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.App.Pages;
using FileBoy.App.Services;
using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IPageNavigationService _pageNavigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource? _thumbnailCts;

    public MainViewModel(
        IFileSystemService fileSystemService,
        INavigationHistory navigationHistory,
        IProcessLauncher processLauncher,
        ISettingsService settingsService,
        IPageNavigationService pageNavigationService,
        IServiceProvider serviceProvider,
        ILogger<MainViewModel> logger)
    {
        _fileSystemService = fileSystemService;
        _navigationHistory = navigationHistory;
        _processLauncher = processLauncher;
        _settingsService = settingsService;
        _pageNavigationService = pageNavigationService;
        _serviceProvider = serviceProvider;
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
    private ImageSource? _previewImage;

    private CancellationTokenSource? _previewCts;

    partial void OnSelectedItemChanged(FileItemViewModel? value)
    {
        _ = LoadPreviewAsync(value);
        UpdateStatusForSelectedItem(value);
    }

    private void UpdateStatusForSelectedItem(FileItemViewModel? item)
    {
        if (item == null)
        {
            StatusText = $"{Items.Count} items";
        }
        else if (item.IsDirectory)
        {
            StatusText = $"{Items.Count} items | Selected: {item.Name} (folder)";
        }
        else
        {
            StatusText = $"{Items.Count} items | Selected: {item.Name} ({item.FormattedSize})";
        }
    }

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
            
            // Load thumbnails if in thumbnail view mode
            if (ViewMode == ViewMode.Thumbnail)
            {
                _ = LoadThumbnailsAsync();
            }
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
            OpenImageViewer(item);
        }
        else
        {
            _processLauncher.OpenWithDefault(item.FullPath);
        }
    }

    private void OpenImageViewer(FileItemViewModel item)
    {
        var allFileItems = Items.Select(i => i.Model).ToList();
        var detailViewModel = _serviceProvider.GetRequiredService<DetailViewModel>();
        detailViewModel.Initialize(item.Model, allFileItems);
        
        var detailPage = new DetailPage(detailViewModel);
        _pageNavigationService.NavigateTo(detailPage);
    }

    [RelayCommand]
    private async Task SetViewMode(string modeStr)
    {
        if (Enum.TryParse<ViewMode>(modeStr, out var mode))
        {
            ViewMode = mode;
            _settingsService.Settings.DefaultView = mode;
            
            if (mode == ViewMode.Thumbnail)
            {
                await LoadThumbnailsAsync();
            }
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await NavigateToPathAsync(CurrentPath);
    }

    private async Task LoadThumbnailsAsync()
    {
        // Cancel any previous thumbnail loading
        _thumbnailCts?.Cancel();
        _thumbnailCts = new CancellationTokenSource();
        var ct = _thumbnailCts.Token;

        var itemsToLoad = Items.Where(i => i.Thumbnail == null && (i.ItemType == FileItemType.Image || i.ItemType == FileItemType.Video)).ToList();
        
        foreach (var item in itemsToLoad)
        {
            if (ct.IsCancellationRequested)
                break;

            try
            {
                if (item.ItemType == FileItemType.Image)
                {
                    await LoadImageThumbnailAsync(item, ct);
                }
                // TODO: Video thumbnails via FFmpeg
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load thumbnail for {Path}", item.FullPath);
            }
        }
    }

    private async Task LoadImageThumbnailAsync(FileItemViewModel item, CancellationToken ct)
    {
        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(item.FullPath);
            bitmap.DecodePixelWidth = 100; // Thumbnail size
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze(); // Required for cross-thread access
            
            // Update on UI thread
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                item.Thumbnail = bitmap;
            });
        }, ct);
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
            
            // Load thumbnails if in thumbnail view mode
            if (ViewMode == ViewMode.Thumbnail)
            {
                _ = LoadThumbnailsAsync();
            }
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

    private async Task LoadPreviewAsync(FileItemViewModel? item)
    {
        // Cancel any previous preview loading
        _previewCts?.Cancel();
        
        if (item == null || !item.IsViewableImage)
        {
            PreviewImage = null;
            return;
        }

        _previewCts = new CancellationTokenSource();
        var ct = _previewCts.Token;

        try
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(item.FullPath);
                bitmap.DecodePixelWidth = 1200; // Preview size
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!ct.IsCancellationRequested)
                    {
                        PreviewImage = bitmap;
                    }
                });
            }, ct);
        }
        catch (OperationCanceledException)
        {
            // Cancelled, ignore
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to load preview for {Path}", item.FullPath);
            PreviewImage = null;
        }
    }
}
