using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.App.Services;
using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.App.ViewModels;

/// <summary>
/// ViewModel for the image detail/viewer page.
/// </summary>
public partial class DetailViewModel : ObservableObject
{
    private readonly IPageNavigationService _navigationService;
    private readonly IImageEditorService _imageEditorService;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<DetailViewModel> _logger;

    public DetailViewModel(
        IPageNavigationService navigationService,
        IImageEditorService imageEditorService,
        ISettingsService settingsService,
        ILogger<DetailViewModel> logger)
    {
        _navigationService = navigationService;
        _imageEditorService = imageEditorService;
        _settingsService = settingsService;
        _logger = logger;
    }

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private ImageSource? _imageSource;

    [ObservableProperty]
    private double _zoomLevel = 1.0;

    [ObservableProperty]
    private string _imageInfo = string.Empty;

    [ObservableProperty]
    private bool _isCropMode;

    [ObservableProperty]
    private Rect _cropSelection;

    [ObservableProperty]
    private string _selectionInfo = string.Empty;

    [ObservableProperty]
    private double _containerWidth;

    [ObservableProperty]
    private double _containerHeight;

    [ObservableProperty]
    private FileItem? _currentFile;

    [ObservableProperty]
    private bool _isVideoPlaying;

    [ObservableProperty]
    private bool _isVideoLoaded;

    [ObservableProperty]
    private TimeSpan _videoDuration;

    [ObservableProperty]
    private TimeSpan _videoPosition;

    [ObservableProperty]
    private bool _isSeekingVideo;

    public bool IsImage => CurrentFile?.IsViewableImage ?? false;
    public bool IsVideo => CurrentFile?.IsVideo ?? false;
    public bool IsUnsupportedFile => CurrentFile != null && !CurrentFile.IsViewableMedia;
    
    /// <summary>
    /// Gets the video display mode as a string for XAML binding to Viewbox Stretch property.
    /// </summary>
    public string VideoDisplayModeStretch
    {
        get
        {
            var mode = _settingsService.Settings.VideoDisplayMode;
            return mode switch
            {
                Core.Enums.MediaDisplayMode.Original => "None",
                Core.Enums.MediaDisplayMode.FitToScreen => "Uniform",
                Core.Enums.MediaDisplayMode.FitIfLarger => "Uniform", // Will be dynamically adjusted in code-behind
                _ => "Uniform"
            };
        }
    }
    
    /// <summary>
    /// Action to trigger video display mode update in the view.
    /// </summary>
    public Action? OnVideoDisplayModeChanged { get; set; }
    
    /// <summary>
    /// Action to stop video playback (called from commands like GoToFirstFrame).
    /// </summary>
    public Action? OnStopVideoRequested { get; set; }

    public bool HasCropSelection => CropSelection.Width > 0 && CropSelection.Height > 0;

    partial void OnCropSelectionChanged(Rect value)
    {
        OnPropertyChanged(nameof(HasCropSelection));
        UpdateSelectionInfo();
    }

    private void UpdateSelectionInfo()
    {
        if (HasCropSelection)
        {
            SelectionInfo = $"Selection: {CropSelection.Width:F0} × {CropSelection.Height:F0} px";
            _logger.LogDebug("Selection updated: {Width}×{Height}", CropSelection.Width, CropSelection.Height);
        }
        else
        {
            SelectionInfo = IsCropMode ? "Click and drag to select area" : string.Empty;
        }
    }

    private CropRectangle ConvertToCropRectangle(Rect rect)
    {
        return new CropRectangle(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private readonly List<FileItem> _allFiles = [];
    private int _currentIndex;

    public bool HasPrevious => _currentIndex > 0;
    public bool HasNext => _currentIndex < _allFiles.Count - 1;

    public void Initialize(FileItem file, IEnumerable<FileItem> allFiles)
    {
        _allFiles.Clear();
        _allFiles.AddRange(allFiles.Where(f => !f.IsDirectory)); // All files except directories
        _currentIndex = _allFiles.FindIndex(f => f.FullPath == file.FullPath);
        
        LoadFile(file);
    }

    public void LoadFile(FileItem file)
    {
        FilePath = file.FullPath;
        FileName = file.Name;
        CurrentFile = file;
        
        // Reset states
        IsCropMode = false;
        CropSelection = Rect.Empty;
        IsVideoPlaying = false;
        IsVideoLoaded = false;
        VideoDuration = TimeSpan.Zero;
        VideoPosition = TimeSpan.Zero;
        IsSeekingVideo = false;
        ImageSource = null;
        ImageInfo = string.Empty;
        
        // Notify file type properties
        OnPropertyChanged(nameof(IsImage));
        OnPropertyChanged(nameof(IsVideo));
        OnPropertyChanged(nameof(IsUnsupportedFile));
        
        if (file.IsViewableImage)
        {
            LoadImage(file);
        }
        else if (file.IsVideo)
        {
            LoadVideo(file);
        }
        else
        {
            // Unsupported file - show file info
            ImageInfo = $"{file.Extension.ToUpperInvariant()} file";
        }

        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
        
        // Notify commands that CanExecute state changed
        PreviousCommand.NotifyCanExecuteChanged();
        NextCommand.NotifyCanExecuteChanged();
    }

    private void LoadImage(FileItem file)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(file.FullPath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            ImageSource = bitmap;
            ImageInfo = $"{bitmap.PixelWidth} × {bitmap.PixelHeight} px";
            
            // Apply display mode
            ApplyDisplayMode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading image: {Path}", file.FullPath);
            ImageInfo = $"Error loading image: {ex.Message}";
        }
    }

    private void LoadVideo(FileItem file)
    {
        // Video will be loaded by MediaElement in XAML
        // Just set info
        var fileInfo = new System.IO.FileInfo(file.FullPath);
        ImageInfo = $"{file.Extension.ToUpperInvariant()} - {Core.Extensions.FileExtensions.FormatFileSize(fileInfo.Length)}";
        _logger.LogInformation("Loading video: {Path}", file.FullPath);
    }

    public void UpdateContainerSize(double width, double height)
    {
        ContainerWidth = width;
        ContainerHeight = height;
        ApplyDisplayMode();
    }

    private void ApplyDisplayMode()
    {
        if (ImageSource is not BitmapSource bitmap || ContainerWidth == 0 || ContainerHeight == 0)
        {
            ZoomLevel = 1.0;
            return;
        }

        var displayMode = _settingsService.Settings.ImageDisplayMode;
        
        switch (displayMode)
        {
            case Core.Enums.MediaDisplayMode.Original:
                ZoomLevel = 1.0;
                break;
                
            case Core.Enums.MediaDisplayMode.FitToScreen:
                ZoomLevel = CalculateFitZoom(bitmap.PixelWidth, bitmap.PixelHeight);
                break;
                
            case Core.Enums.MediaDisplayMode.FitIfLarger:
                if (bitmap.PixelWidth > ContainerWidth || bitmap.PixelHeight > ContainerHeight)
                {
                    ZoomLevel = CalculateFitZoom(bitmap.PixelWidth, bitmap.PixelHeight);
                }
                else
                {
                    ZoomLevel = 1.0;
                }
                break;
        }
    }

    private double CalculateFitZoom(int imageWidth, int imageHeight)
    {
        var scaleX = ContainerWidth / imageWidth;
        var scaleY = ContainerHeight / imageHeight;
        return Math.Min(scaleX, scaleY);
    }

    [RelayCommand]
    private void GoBackToBrowser()
    {
        _navigationService.GoBack();
    }

    [RelayCommand(CanExecute = nameof(HasPrevious))]
    private void Previous()
    {
        if (_currentIndex > 0)
        {
            _currentIndex--;
            LoadFile(_allFiles[_currentIndex]);
            PreviousCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(HasNext))]
    private void Next()
    {
        if (_currentIndex < _allFiles.Count - 1)
        {
            _currentIndex++;
            LoadFile(_allFiles[_currentIndex]);
            PreviousCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void DeleteFile()
    {
        if (CurrentFile == null)
            return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete this file?\n\n{FileName}\n\nThis action cannot be undone.",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            var fileToDelete = CurrentFile.FullPath;
            
            // Determine where to navigate after deletion
            bool hasMoreFiles = _allFiles.Count > 1;
            bool wasLastFile = _currentIndex == _allFiles.Count - 1;
            
            // Remove from list
            _allFiles.RemoveAt(_currentIndex);
            
            // Delete the file
            File.Delete(fileToDelete);
            _logger.LogInformation("Deleted file: {Path}", fileToDelete);
            
            // Navigate to appropriate file or go back
            if (!hasMoreFiles)
            {
                // No files left, go back to browser
                _navigationService.GoBack();
            }
            else if (wasLastFile)
            {
                // Was last file, move to previous (which is now at the same index - 1)
                _currentIndex--;
                LoadFile(_allFiles[_currentIndex]);
            }
            else
            {
                // Move to next file (which is now at the same index)
                LoadFile(_allFiles[_currentIndex]);
            }
            
            // Update command states
            PreviousCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {Path}", CurrentFile.FullPath);
            MessageBox.Show(
                $"Failed to delete file:\n{ex.Message}",
                "Delete Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ZoomIn()
    {
        ZoomLevel = Math.Min(ZoomLevel * 1.25, 10.0);
    }

    [RelayCommand]
    private void ZoomOut()
    {
        ZoomLevel = Math.Max(ZoomLevel / 1.25, 0.1);
    }

    [RelayCommand]
    private void ZoomFit()
    {
        ZoomLevel = 1.0;
    }

    [RelayCommand]
    private void ToggleCropMode()
    {
        IsCropMode = !IsCropMode;
        _logger.LogInformation("Crop mode toggled: {CropMode}", IsCropMode);
        if (!IsCropMode)
        {
            CropSelection = Rect.Empty;
            _logger.LogInformation("Crop selection cleared");
        }
        UpdateSelectionInfo();
    }

    [RelayCommand]
    private async Task CropImageAsync()
    {
        if (!HasCropSelection)
        {
            _logger.LogWarning("Crop command called without selection");
            return;
        }

        try
        {
            // Show save dialog
            var dialog = new Views.SaveImageDialog(FilePath, "Save Cropped Image")
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            // Perform crop
            var success = await _imageEditorService.CropImageAsync(
                FilePath, 
                dialog.FullPath, 
                ConvertToCropRectangle(CropSelection));

            if (success)
            {
                MessageBox.Show(
                    $"Image saved successfully to:\n{dialog.FullPath}",
                    "Crop Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Reset crop mode
                IsCropMode = false;
                CropSelection = Rect.Empty;

                _logger.LogInformation("Successfully cropped image to {Path}", dialog.FullPath);
            }
            else
            {
                MessageBox.Show(
                    "Failed to crop image. Please try again.",
                    "Crop Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during crop operation");
            MessageBox.Show(
                $"Failed to crop image: {ex.Message}",
                "Crop Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ResizeImageAsync()
    {
        if (ImageSource is not BitmapSource bitmap)
        {
            _logger.LogWarning("Resize command called without valid image");
            return;
        }

        try
        {
            // Show resize dialog
            var dialog = new Views.ResizeImageDialog(bitmap.PixelWidth, bitmap.PixelHeight)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            // Show save dialog
            var saveDialog = new Views.SaveImageDialog(FilePath, "Save Resized Image")
            {
                Owner = Application.Current.MainWindow
            };

            if (saveDialog.ShowDialog() != true)
            {
                return;
            }

            // Perform resize
            var success = await _imageEditorService.ResizeImageAsync(
                FilePath,
                saveDialog.FullPath,
                dialog.NewWidth,
                dialog.NewHeight);

            if (success)
            {
                MessageBox.Show(
                    $"Image resized successfully to:\n{saveDialog.FullPath}\nNew size: {dialog.NewWidth} × {dialog.NewHeight} px",
                    "Resize Successful",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                _logger.LogInformation("Successfully resized image to {Path} ({Width}×{Height})", 
                    saveDialog.FullPath, dialog.NewWidth, dialog.NewHeight);
            }
            else
            {
                MessageBox.Show(
                    "Failed to resize image. Please try again.",
                    "Resize Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resize operation");
            MessageBox.Show(
                $"Failed to resize image: {ex.Message}",
                "Resize Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task SetDisplayMode(string mode)
    {
        if (Enum.TryParse<Core.Enums.MediaDisplayMode>(mode, out var displayMode))
        {
            if (IsImage)
            {
                _settingsService.Settings.ImageDisplayMode = displayMode;
                await _settingsService.SaveAsync();
                ApplyDisplayMode();
                _logger.LogInformation("Image display mode changed to {Mode}", displayMode);
            }
            else if (IsVideo)
            {
                _settingsService.Settings.VideoDisplayMode = displayMode;
                await _settingsService.SaveAsync();
                OnPropertyChanged(nameof(VideoDisplayModeStretch));
                OnVideoDisplayModeChanged?.Invoke();
                _logger.LogInformation("Video display mode changed to {Mode}", displayMode);
            }
        }
    }

    [RelayCommand]
    private void TogglePlayPause()
    {
        IsVideoPlaying = !IsVideoPlaying;
    }

    [RelayCommand]
    private void GoToFirstFrame()
    {
        if (!IsVideo || VideoDuration == TimeSpan.Zero)
            return;
            
        IsVideoPlaying = false;
        OnStopVideoRequested?.Invoke();
        OnSeekRequested?.Invoke(TimeSpan.Zero);
        _logger.LogInformation("Jumped to first frame");
    }
    
    [RelayCommand]
    private void GoToLastFrame()
    {
        if (!IsVideo || VideoDuration == TimeSpan.Zero)
            return;
            
        IsVideoPlaying = false;
        OnStopVideoRequested?.Invoke();
        // Go to last frame (just before the very end to ensure frame is visible)
        var lastFrame = VideoDuration - TimeSpan.FromMilliseconds(100);
        OnSeekRequested?.Invoke(lastFrame);
        _logger.LogInformation("Jumped to last frame");
    }

    [RelayCommand]
    private void SeekBackward()
    {
        if (!IsVideo || VideoDuration == TimeSpan.Zero)
            return;

        var interval = TimeSpan.FromSeconds(_settingsService.Settings.VideoSeekInterval);
        var newPosition = VideoPosition - interval;
        if (newPosition < TimeSpan.Zero)
            newPosition = TimeSpan.Zero;

        VideoPosition = newPosition;
        OnSeekRequested?.Invoke(newPosition);
        _logger.LogDebug("Seeking backward to {Position}", newPosition);
    }

    [RelayCommand]
    private void SeekForward()
    {
        if (!IsVideo || VideoDuration == TimeSpan.Zero)
            return;

        var interval = TimeSpan.FromSeconds(_settingsService.Settings.VideoSeekInterval);
        var newPosition = VideoPosition + interval;
        if (newPosition > VideoDuration)
            newPosition = VideoDuration;

        VideoPosition = newPosition;
        OnSeekRequested?.Invoke(newPosition);
        _logger.LogDebug("Seeking forward to {Position}", newPosition);
    }

    public Action<TimeSpan>? OnSeekRequested { get; set; }

    [RelayCommand]
    private void TakeSnapshot()
    {
        if (!IsVideo)
        {
            _logger.LogWarning("Snapshot requested but current file is not a video");
            return;
        }

        var snapshotFolder = _settingsService.Settings.SnapshotFolder;
        
        if (string.IsNullOrWhiteSpace(snapshotFolder))
        {
            MessageBox.Show(
                "Snapshot folder is not configured. Please set the snapshot folder in Settings.",
                "Snapshot Folder Not Set",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            _logger.LogWarning("Snapshot attempted but folder not configured");
            return;
        }

        if (!Directory.Exists(snapshotFolder))
        {
            try
            {
                Directory.CreateDirectory(snapshotFolder);
                _logger.LogInformation("Created snapshot folder: {Folder}", snapshotFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create snapshot folder: {Folder}", snapshotFolder);
                MessageBox.Show(
                    $"Failed to create snapshot folder: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        // Request snapshot from view
        OnSnapshotRequested?.Invoke();
    }

    public Action? OnSnapshotRequested { get; set; }

    [RelayCommand]
    private void OpenInAssociatedApp()
    {
        if (string.IsNullOrEmpty(FilePath))
            return;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = FilePath,
                UseShellExecute = true
            };
            Process.Start(psi);
            _logger.LogInformation("Opened file in associated application: {Path}", FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in associated application: {Path}", FilePath);
            MessageBox.Show(
                $"Failed to open file: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void OnVideoLoaded()
    {
        IsVideoLoaded = true;
        _logger.LogInformation("Video loaded successfully");
    }

    public void OnVideoFailed(string error)
    {
        _logger.LogError("Video failed to load: {Error}", error);
        ImageInfo = $"Error loading video: {error}";
    }
}
