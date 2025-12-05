using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.App.Services;
using FileBoy.Core.Models;

namespace FileBoy.App.ViewModels;

/// <summary>
/// ViewModel for the image detail/viewer page.
/// </summary>
public partial class DetailViewModel : ObservableObject
{
    private readonly IPageNavigationService _navigationService;

    public DetailViewModel(IPageNavigationService navigationService)
    {
        _navigationService = navigationService;
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
        
        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(file.FullPath);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
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
}
