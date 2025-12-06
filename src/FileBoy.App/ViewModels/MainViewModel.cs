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
    private readonly IVideoThumbnailService _videoThumbnailService;
    private readonly IFFmpegManager _ffmpegManager;
    private readonly IClipboardService _clipboardService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MainViewModel> _logger;
    private CancellationTokenSource? _thumbnailCts;

    public MainViewModel(
        IFileSystemService fileSystemService,
        INavigationHistory navigationHistory,
        IProcessLauncher processLauncher,
        ISettingsService settingsService,
        IPageNavigationService pageNavigationService,
        IVideoThumbnailService videoThumbnailService,
        IFFmpegManager ffmpegManager,
        IClipboardService clipboardService,
        IServiceProvider serviceProvider,
        ILogger<MainViewModel> logger)
    {
        _fileSystemService = fileSystemService;
        _navigationHistory = navigationHistory;
        _processLauncher = processLauncher;
        _settingsService = settingsService;
        _pageNavigationService = pageNavigationService;
        _videoThumbnailService = videoThumbnailService;
        _ffmpegManager = ffmpegManager;
        _clipboardService = clipboardService;
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
    private System.Collections.IList? _selectedItems;

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

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ThumbnailContainerSize))]
    private int _thumbnailDisplaySize = 120;

    /// <summary>
    /// Container size is slightly larger than thumbnail for padding.
    /// </summary>
    public int ThumbnailContainerSize => ThumbnailDisplaySize + 20;

    [ObservableProperty]
    private double _previewPanelWidth = 400;

    partial void OnPreviewPanelWidthChanged(double value)
    {
        // Save the width when it changes
        _settingsService.Settings.PreviewPanelWidth = value;
        _ = _settingsService.SaveAsync();
    }

    public bool CanGoBack => _navigationHistory.CanGoBack;
    public bool CanGoForward => _navigationHistory.CanGoForward;

    public async Task InitializeAsync()
    {
        await _settingsService.LoadAsync();
        ViewMode = _settingsService.Settings.DefaultView;
        ThumbnailDisplaySize = _settingsService.Settings.ThumbnailSize;
        PreviewPanelWidth = _settingsService.Settings.PreviewPanelWidth;
        
        // Ensure FFmpeg is available (downloads if needed)
        if (!_ffmpegManager.IsAvailable)
        {
            _logger.LogInformation("FFmpeg not found, attempting to download...");
            StatusText = "Downloading FFmpeg for video thumbnails...";
            await _ffmpegManager.EnsureAvailableAsync();
        }
        
        if (_ffmpegManager.IsAvailable)
        {
            _logger.LogInformation("FFmpeg is available at: {Path}", _ffmpegManager.FFmpegPath);
        }
        
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

    [RelayCommand]
    private void CopyFullPath()
    {
        if (SelectedItem == null)
            return;

        try
        {
            System.Windows.Clipboard.SetText(SelectedItem.FullPath);
            StatusText = $"Copied: {SelectedItem.FullPath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy path to clipboard");
        }
    }

    [RelayCommand]
    private void Cut()
    {
        var selectedPaths = GetSelectedPaths();
        _logger.LogInformation("Cut command called with {Count} selected items", selectedPaths.Count);
        
        if (selectedPaths.Count == 0)
        {
            _logger.LogWarning("No items selected for cut");
            return;
        }

        try
        {
            _clipboardService.Cut(selectedPaths);
            
            // Also set Windows clipboard
            var fileCollection = new System.Collections.Specialized.StringCollection();
            fileCollection.AddRange(selectedPaths.ToArray());
            System.Windows.Clipboard.Clear();
            System.Windows.Clipboard.SetFileDropList(fileCollection);
            
            // Set preferred drop effect to move (cut)
            var dropEffect = new System.IO.MemoryStream(4);
            dropEffect.Write(BitConverter.GetBytes(2), 0, 4); // 2 = DROPEFFECT_MOVE
            dropEffect.Position = 0;
            System.Windows.Clipboard.SetData("Preferred DropEffect", dropEffect);
            
            StatusText = $"Cut {selectedPaths.Count} item(s) to clipboard";
            _logger.LogInformation("Cut {Count} items successfully", selectedPaths.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cut items");
            StatusText = "Failed to cut items";
        }
    }

    [RelayCommand]
    private void Copy()
    {
        var selectedPaths = GetSelectedPaths();
        _logger.LogInformation("Copy command called with {Count} selected items", selectedPaths.Count);
        
        if (selectedPaths.Count == 0)
        {
            _logger.LogWarning("No items selected for copy");
            return;
        }

        try
        {
            _clipboardService.Copy(selectedPaths);
            
            // Also set Windows clipboard
            var fileCollection = new System.Collections.Specialized.StringCollection();
            fileCollection.AddRange(selectedPaths.ToArray());
            System.Windows.Clipboard.Clear();
            System.Windows.Clipboard.SetFileDropList(fileCollection);
            
            StatusText = $"Copied {selectedPaths.Count} item(s) to clipboard";
            _logger.LogInformation("Copied {Count} items successfully", selectedPaths.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy items");
            StatusText = "Failed to copy items";
        }
    }

    [RelayCommand]
    private async Task PasteAsync()
    {
        _logger.LogInformation("PasteAsync command called");
        
        if (!_clipboardService.CanPaste())
        {
            _logger.LogWarning("CanPaste returned false - nothing to paste");
            StatusText = "Nothing to paste";
            return;
        }

        var clipboardData = _clipboardService.GetClipboardData();
        
        try
        {
            IsLoading = true;

            if (clipboardData.Operation == Core.Enums.ClipboardOperation.Copy)
            {
                StatusText = $"Copying {clipboardData.FilePaths.Count} item(s)...";
                _logger.LogInformation("Starting copy of {Count} items to {Destination}", clipboardData.FilePaths.Count, CurrentPath);
                await _fileSystemService.CopyFilesAsync(clipboardData.FilePaths, CurrentPath);
                StatusText = $"Successfully copied {clipboardData.FilePaths.Count} item(s)";
                _logger.LogInformation("Copy completed successfully");
            }
            else if (clipboardData.Operation == Core.Enums.ClipboardOperation.Cut)
            {
                StatusText = $"Moving {clipboardData.FilePaths.Count} item(s)...";
                _logger.LogInformation("Starting move of {Count} items to {Destination}", clipboardData.FilePaths.Count, CurrentPath);
                await _fileSystemService.MoveFilesAsync(clipboardData.FilePaths, CurrentPath);
                StatusText = $"Successfully moved {clipboardData.FilePaths.Count} item(s)";
                _logger.LogInformation("Move completed successfully");
                _clipboardService.Clear();
            }

            // Refresh the current directory
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to paste items");
            StatusText = $"Failed to paste items: {ex.Message}";
            System.Windows.MessageBox.Show(
                $"Failed to paste items: {ex.Message}",
                "Paste Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        var selectedPaths = GetSelectedPaths();
        if (selectedPaths.Count == 0)
            return;

        // Ask for confirmation
        var message = selectedPaths.Count == 1
            ? $"Are you sure you want to delete '{Path.GetFileName(selectedPaths[0])}'?"
            : $"Are you sure you want to delete {selectedPaths.Count} items?";

        var result = System.Windows.MessageBox.Show(
            message,
            "Confirm Delete",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
            return;

        try
        {
            IsLoading = true;
            StatusText = $"Deleting {selectedPaths.Count} item(s)...";

            await _fileSystemService.DeleteFilesAsync(selectedPaths);

            StatusText = $"Successfully deleted {selectedPaths.Count} item(s)";
            _logger.LogInformation("Deleted {Count} items", selectedPaths.Count);

            // Refresh the current directory
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete items");
            StatusText = "Failed to delete items";
            System.Windows.MessageBox.Show(
                $"Failed to delete items: {ex.Message}",
                "Delete Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task NewFolderAsync()
    {
        try
        {
            var dialog = new Views.NewFolderDialog
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusText = $"Creating folder '{dialog.FolderName}'...";

                var createdPath = await _fileSystemService.CreateFolderAsync(CurrentPath, dialog.FolderName);
                
                StatusText = $"Created folder: {Path.GetFileName(createdPath)}";
                _logger.LogInformation("Created folder: {Path}", createdPath);

                // Refresh to show the new folder
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create folder");
            StatusText = "Failed to create folder";
            System.Windows.MessageBox.Show(
                $"Failed to create folder: {ex.Message}",
                "Create Folder Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanRename))]
    private async Task RenameAsync()
    {
        try
        {
            if (SelectedItem == null) return;

            var currentName = SelectedItem.Name;
            var isDirectory = SelectedItem.IsDirectory;

            var dialog = new Views.RenameDialog(currentName, isDirectory)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusText = $"Renaming '{currentName}' to '{dialog.NewName}'...";

                var newPath = await _fileSystemService.RenameAsync(SelectedItem.FullPath, dialog.NewName);
                
                StatusText = $"Renamed to: {Path.GetFileName(newPath)}";
                _logger.LogInformation("Renamed {Old} to {New}", SelectedItem.FullPath, newPath);

                // Refresh to show the renamed item
                await RefreshAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rename item");
            StatusText = "Failed to rename item";
            System.Windows.MessageBox.Show(
                $"Failed to rename item: {ex.Message}",
                "Rename Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private bool CanRename() => SelectedItem != null;

    private List<string> GetSelectedPaths()
    {
        if (SelectedItems != null && SelectedItems.Count > 0)
        {
            var paths = SelectedItems.Cast<FileItemViewModel>().Select(i => i.FullPath).ToList();
            _logger.LogDebug("GetSelectedPaths from SelectedItems: {Count} items", paths.Count);
            return paths;
        }
        else if (SelectedItem != null)
        {
            _logger.LogDebug("GetSelectedPaths from SelectedItem: {Path}", SelectedItem.FullPath);
            return [SelectedItem.FullPath];
        }

        _logger.LogDebug("GetSelectedPaths: No selection");
        return [];
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var settingsViewModel = _serviceProvider.GetRequiredService<SettingsViewModel>();
        var settingsWindow = new Views.SettingsWindow(settingsViewModel);
        settingsWindow.Owner = System.Windows.Application.Current.MainWindow;
        
        var oldThumbnailSize = _settingsService.Settings.ThumbnailSize;
        
        if (settingsWindow.ShowDialog() == true)
        {
            // Update display size binding
            ThumbnailDisplaySize = _settingsService.Settings.ThumbnailSize;
            
            // Settings were saved, check if thumbnail size changed
            if (ViewMode == ViewMode.Thumbnail && oldThumbnailSize != _settingsService.Settings.ThumbnailSize)
            {
                // Clear existing thumbnails and reload with new size
                foreach (var item in Items)
                {
                    item.Thumbnail = null;
                }
                _ = LoadThumbnailsAsync();
            }
        }
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
                else if (item.ItemType == FileItemType.Video)
                {
                    await LoadVideoThumbnailAsync(item, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to load thumbnail for {Path}", item.FullPath);
            }
        }
    }

    private async Task LoadVideoThumbnailAsync(FileItemViewModel item, CancellationToken ct)
    {
        var thumbnailSize = _settingsService.Settings.ThumbnailSize;
        
        var thumbnailData = await _videoThumbnailService.GetThumbnailAsync(item.FullPath, thumbnailSize, ct);
        
        if (thumbnailData != null && thumbnailData.Length > 0)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    using var stream = new MemoryStream(thumbnailData);
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    item.Thumbnail = bitmap;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to create bitmap from video thumbnail for {Path}", item.FullPath);
                }
            });
        }
    }

    private async Task LoadImageThumbnailAsync(FileItemViewModel item, CancellationToken ct)
    {
        var thumbnailSize = _settingsService.Settings.ThumbnailSize;
        
        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(item.FullPath);
            bitmap.DecodePixelWidth = thumbnailSize; // Use settings thumbnail size
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
