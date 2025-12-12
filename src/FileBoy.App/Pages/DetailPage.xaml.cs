using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FileBoy.App.ViewModels;
using FileBoy.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FileBoy.App.Pages;

/// <summary>
/// Interaction logic for DetailPage.xaml
/// </summary>
public partial class DetailPage : Page
{
    private DetailViewModel ViewModel => (DetailViewModel)DataContext;
    private Point _selectionStart;
    private bool _isSelecting;
    private DispatcherTimer? _videoPositionTimer;
    private bool _isUserSeekingVideo;

    public DetailPage(DetailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Ensure page can receive keyboard input
        Focusable = true;
        Loaded += (s, e) => Focus();
        
        // Track container size changes
        ImageScrollViewer.SizeChanged += (s, e) =>
        {
            if (e.NewSize.Width > 0 && e.NewSize.Height > 0)
            {
                viewModel.UpdateContainerSize(e.NewSize.Width, e.NewSize.Height);
            }
        };
        
        // Setup video position timer
        _videoPositionTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _videoPositionTimer.Tick += VideoPositionTimer_Tick;
        
        // Setup video seek callback
        viewModel.OnSeekRequested = (position) =>
        {
            VideoPlayer.Position = position;
        };
        
        // Setup snapshot callback
        viewModel.OnSnapshotRequested = () =>
        {
            CaptureVideoSnapshot();
        };
        
        // Cleanup on unload
        Unloaded += (s, e) => _videoPositionTimer?.Stop();
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Delta > 0)
                ViewModel.ZoomInCommand.Execute(null);
            else
                ViewModel.ZoomOutCommand.Execute(null);
            
            e.Handled = true;
        }
    }

    private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!ViewModel.IsCropMode)
        {
            System.Diagnostics.Debug.WriteLine("[Crop] Mouse down - crop mode is OFF");
            return;
        }

        _isSelecting = true;
        _selectionStart = e.GetPosition(MainImage);
        
        System.Diagnostics.Debug.WriteLine($"[Crop] Mouse down at ({_selectionStart.X:F0}, {_selectionStart.Y:F0})");
        System.Diagnostics.Debug.WriteLine($"[Crop] Image ActualSize: {MainImage.ActualWidth:F0} × {MainImage.ActualHeight:F0}");
        System.Diagnostics.Debug.WriteLine($"[Crop] Canvas Size: {CropCanvas.ActualWidth:F0} × {CropCanvas.ActualHeight:F0}");
        System.Diagnostics.Debug.WriteLine($"[Crop] Canvas Visibility: {CropCanvas.Visibility}");
        
        SelectionRect.Visibility = Visibility.Visible;
        Canvas.SetLeft(SelectionRect, _selectionStart.X);
        Canvas.SetTop(SelectionRect, _selectionStart.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        
        System.Diagnostics.Debug.WriteLine($"[Crop] SelectionRect visibility set to: {SelectionRect.Visibility}");
        
        MainImage.CaptureMouse();
        e.Handled = true;
    }

    private void Image_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting || !ViewModel.IsCropMode)
            return;

        var currentPos = e.GetPosition(MainImage);
        
        var x = Math.Min(_selectionStart.X, currentPos.X);
        var y = Math.Min(_selectionStart.Y, currentPos.Y);
        var width = Math.Abs(currentPos.X - _selectionStart.X);
        var height = Math.Abs(currentPos.Y - _selectionStart.Y);
        
        // Constrain to image bounds
        var imageWidth = MainImage.ActualWidth;
        var imageHeight = MainImage.ActualHeight;
        
        x = Math.Max(0, Math.Min(x, imageWidth));
        y = Math.Max(0, Math.Min(y, imageHeight));
        width = Math.Min(width, imageWidth - x);
        height = Math.Min(height, imageHeight - y);
        
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = width;
        SelectionRect.Height = height;
        
        System.Diagnostics.Debug.WriteLine($"[Crop] Mouse move - Rect: ({x:F0}, {y:F0}) {width:F0} × {height:F0}");
    }

    private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting || !ViewModel.IsCropMode)
            return;

        _isSelecting = false;
        MainImage.ReleaseMouseCapture();
        
        System.Diagnostics.Debug.WriteLine($"[Crop] Mouse up - finalizing selection");
        
        // Calculate selection in actual image pixels (accounting for zoom and actual bitmap size)
        if (ViewModel.ImageSource is BitmapSource bitmap)
        {
            var imageDisplayWidth = MainImage.ActualWidth;
            var imageDisplayHeight = MainImage.ActualHeight;
            
            var scaleX = bitmap.PixelWidth / imageDisplayWidth;
            var scaleY = bitmap.PixelHeight / imageDisplayHeight;
            
            var left = Canvas.GetLeft(SelectionRect);
            var top = Canvas.GetTop(SelectionRect);
            var width = SelectionRect.Width;
            var height = SelectionRect.Height;
            
            System.Diagnostics.Debug.WriteLine($"[Crop] Display rect: ({left:F0}, {top:F0}) {width:F0} × {height:F0}");
            System.Diagnostics.Debug.WriteLine($"[Crop] Bitmap size: {bitmap.PixelWidth} × {bitmap.PixelHeight}");
            System.Diagnostics.Debug.WriteLine($"[Crop] Scale factors: {scaleX:F2} × {scaleY:F2}");
            
            // Convert to actual image coordinates
            var cropRect = new Rect(
                left * scaleX,
                top * scaleY,
                width * scaleX,
                height * scaleY
            );
            
            System.Diagnostics.Debug.WriteLine($"[Crop] Final crop rect (image pixels): ({cropRect.X:F0}, {cropRect.Y:F0}) {cropRect.Width:F0} × {cropRect.Height:F0}");
            
            ViewModel.CropSelection = cropRect;
        }
        
        e.Handled = true;
    }

    private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e)
    {
        ViewModel.OnVideoLoaded();
        
        // Set video duration
        if (VideoPlayer.NaturalDuration.HasTimeSpan)
        {
            ViewModel.VideoDuration = VideoPlayer.NaturalDuration.TimeSpan;
        }
        
        // LoadedBehavior="Pause" automatically shows first frame
        ViewModel.VideoPosition = TimeSpan.Zero;
        
        // Initialize volume controls only if not already initialized
        if (VolumeSlider.Value == 1.0 && VideoPlayer.Volume == 0)
        {
            VideoPlayer.Volume = 0; // Start muted
            VolumeSlider.Value = 1.0; // Default volume when unmuted
            UpdateMuteButtonIcon();
        }
        
        // Only reset play state if video is not currently playing
        // (MediaOpened can fire multiple times)
        if (!ViewModel.IsVideoPlaying)
        {
            PlayPauseButton.Content = "▶ Play";
        }
    }

    private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        ViewModel.OnVideoFailed(e.ErrorException?.Message ?? "Unknown error");
        _videoPositionTimer?.Stop();
    }

    private void VideoPositionTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isUserSeekingVideo && VideoPlayer.Source != null)
        {
            ViewModel.VideoPosition = VideoPlayer.Position;
        }
    }

    private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
    {
        TogglePlayPause();
    }

    private void VideoPlayer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        TogglePlayPause();
        e.Handled = true;
    }

    private void TogglePlayPause()
    {
        VideoPlayer.LoadedBehavior = MediaState.Manual;
        
        if (ViewModel.IsVideoPlaying)
        {
            VideoPlayer.Pause();
            ViewModel.IsVideoPlaying = false;
            _videoPositionTimer?.Stop();
            PlayPauseButton.Content = "▶ Play";
        }
        else
        {
            // Restore volume if it was muted initially
            if (VideoPlayer.Volume == 0)
            {
                VideoPlayer.Volume = VolumeSlider.Value;
            }
            VideoPlayer.Play();
            ViewModel.IsVideoPlaying = true;
            _videoPositionTimer?.Start();
            PlayPauseButton.Content = "⏸ Pause";
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        VideoPlayer.LoadedBehavior = MediaState.Manual;
        VideoPlayer.Stop();
        VideoPlayer.Position = TimeSpan.Zero;
        ViewModel.IsVideoPlaying = false;
        ViewModel.VideoPosition = TimeSpan.Zero;
        _videoPositionTimer?.Stop();
        PlayPauseButton.Content = "▶ Play";
    }

    private void VideoProgressSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isUserSeekingVideo = true;
        _videoPositionTimer?.Stop();
    }

    private void VideoProgressSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isUserSeekingVideo = false;
        
        // Seek to new position
        var newPosition = TimeSpan.FromSeconds(VideoProgressSlider.Value);
        VideoPlayer.Position = newPosition;
        ViewModel.VideoPosition = newPosition;
        
        // Resume timer if video was playing
        if (ViewModel.IsVideoPlaying)
        {
            _videoPositionTimer?.Start();
        }
    }

    private void VideoProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Handle both dragging and direct clicks
        if (_isUserSeekingVideo)
        {
            var newPosition = TimeSpan.FromSeconds(e.NewValue);
            VideoPlayer.Position = newPosition;
            ViewModel.VideoPosition = newPosition;
        }
        // Handle direct click on slider (IsMoveToPointEnabled)
        else if (VideoPlayer.Source != null && Math.Abs(e.NewValue - ViewModel.VideoPosition.TotalSeconds) > 0.5)
        {
            // User clicked directly on slider without dragging
            var wasPlaying = ViewModel.IsVideoPlaying;
            _videoPositionTimer?.Stop();
            
            var newPosition = TimeSpan.FromSeconds(e.NewValue);
            VideoPlayer.Position = newPosition;
            ViewModel.VideoPosition = newPosition;
            
            if (wasPlaying)
            {
                _videoPositionTimer?.Start();
            }
        }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VideoPlayer != null)
        {
            VideoPlayer.Volume = e.NewValue;
            UpdateMuteButtonIcon();
        }
    }

    private void MuteButton_Click(object sender, RoutedEventArgs e)
    {
        if (VideoPlayer.Volume > 0)
        {
            // Mute
            VolumeSlider.Tag = VideoPlayer.Volume; // Store current volume
            VideoPlayer.Volume = 0;
            VolumeSlider.Value = 0;
        }
        else
        {
            // Unmute - restore previous volume or default to 1.0
            var previousVolume = VolumeSlider.Tag as double? ?? 1.0;
            VideoPlayer.Volume = previousVolume;
            VolumeSlider.Value = previousVolume;
        }
        UpdateMuteButtonIcon();
    }

    private void UpdateMuteButtonIcon()
    {
        if (MuteButton != null)
        {
            MuteButton.Content = VideoPlayer.Volume == 0 ? "🔇" : "🔊";
        }
    }

    private void CaptureVideoSnapshot()
    {
        try
        {
            // Get snapshot settings
            var settingsService = App.Services.GetRequiredService<ISettingsService>();
            var settings = settingsService.Settings;
            
            var snapshotFolder = settings.SnapshotFolder;
            var nameTemplate = settings.SnapshotNameTemplate;
            
            // Generate filename
            var videoName = System.IO.Path.GetFileNameWithoutExtension(ViewModel.FileName);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            
            // Find next available counter
            var counter = 1;
            string fileName;
            string fullPath;
            
            do
            {
                fileName = nameTemplate
                    .Replace("{name}", videoName)
                    .Replace("{counter}", counter.ToString())
                    .Replace("{timestamp}", timestamp) + ".png";
                
                fullPath = System.IO.Path.Combine(snapshotFolder, fileName);
                counter++;
            } while (System.IO.File.Exists(fullPath) && counter < 1000);
            
            // Capture the video frame
            var width = (int)VideoPlayer.ActualWidth;
            var height = (int)VideoPlayer.ActualHeight;
            
            if (width == 0 || height == 0)
            {
                MessageBox.Show("Cannot capture snapshot: video player size is invalid.", "Snapshot Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            var renderBitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            
            renderBitmap.Render(VideoPlayer);
            
            // Save as PNG
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(renderBitmap));
            
            using (var fileStream = new System.IO.FileStream(fullPath, System.IO.FileMode.Create))
            {
                encoder.Save(fileStream);
            }

        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to capture snapshot: {ex.Message}",
                "Snapshot Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
