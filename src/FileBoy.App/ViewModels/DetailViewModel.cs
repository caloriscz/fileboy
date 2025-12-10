using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.App.Services;
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
    private readonly ILogger<DetailViewModel> _logger;

    public DetailViewModel(
        IPageNavigationService navigationService,
        IImageEditorService imageEditorService,
        ILogger<DetailViewModel> logger)
    {
        _navigationService = navigationService;
        _imageEditorService = imageEditorService;
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

    private readonly List<FileItem> _imageFiles = [];
    private int _currentIndex;

    public bool HasPrevious => _currentIndex > 0;
    public bool HasNext => _currentIndex < _imageFiles.Count - 1;

    public void Initialize(FileItem file, IEnumerable<FileItem> allFiles)
    {
        _imageFiles.Clear();
        _imageFiles.AddRange(allFiles.Where(f => f.IsViewableImage));
        _currentIndex = _imageFiles.FindIndex(f => f.FullPath == file.FullPath);
        
        LoadImage(file);
    }

    public void LoadImage(FileItem file)
    {
        FilePath = file.FullPath;
        FileName = file.Name;
        
        // Reset crop mode when loading new image
        IsCropMode = false;
        CropSelection = Rect.Empty;
        
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
            ZoomLevel = 1.0;
        }
        catch (Exception ex)
        {
            ImageInfo = $"Error loading image: {ex.Message}";
        }

        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
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
            LoadImage(_imageFiles[_currentIndex]);
        }
    }

    [RelayCommand(CanExecute = nameof(HasNext))]
    private void Next()
    {
        if (_currentIndex < _imageFiles.Count - 1)
        {
            _currentIndex++;
            LoadImage(_imageFiles[_currentIndex]);
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
        MessageBox.Show($"Crop mode clicked! Current: {IsCropMode}", "Debug", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var dialog = new Views.SaveImageDialog(FilePath)
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
}
