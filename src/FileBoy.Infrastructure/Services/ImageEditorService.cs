using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Service for image editing operations.
/// </summary>
public sealed class ImageEditorService : IImageEditorService
{
    private readonly ILogger<ImageEditorService> _logger;

    public ImageEditorService(ILogger<ImageEditorService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> CropImageAsync(string sourceImagePath, string destinationPath, CropRectangle cropRect, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Cropping image: {Source} to {Destination}, rect: {Rect}", 
                sourceImagePath, destinationPath, cropRect);

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                // Load source image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(sourceImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Ensure crop rectangle is within bounds
                var validRect = new Int32Rect(
                    Math.Max(0, (int)cropRect.X),
                    Math.Max(0, (int)cropRect.Y),
                    Math.Min((int)cropRect.Width, bitmap.PixelWidth - (int)cropRect.X),
                    Math.Min((int)cropRect.Height, bitmap.PixelHeight - (int)cropRect.Y)
                );

                // Create cropped bitmap
                var croppedBitmap = new CroppedBitmap(bitmap, validRect);

                // Determine encoder based on file extension
                BitmapEncoder encoder = Path.GetExtension(destinationPath).ToLowerInvariant() switch
                {
                    ".png" => new PngBitmapEncoder(),
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                    ".bmp" => new BmpBitmapEncoder(),
                    ".gif" => new GifBitmapEncoder(),
                    ".tiff" or ".tif" => new TiffBitmapEncoder(),
                    _ => new PngBitmapEncoder() // Default to PNG
                };

                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));

                // Save to file
                using var fileStream = new FileStream(destinationPath, FileMode.Create);
                encoder.Save(fileStream);

                _logger.LogInformation("Successfully cropped image to {Destination}", destinationPath);
            }, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to crop image from {Source} to {Destination}", sourceImagePath, destinationPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ResizeImageAsync(string sourceImagePath, string destinationPath, int width, int height, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Resizing image: {Source} to {Destination}, size: {Width}×{Height}", 
                sourceImagePath, destinationPath, width, height);

            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                // Load source image
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(sourceImagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                // Create resized bitmap using TransformedBitmap with ScaleTransform
                var scaleX = (double)width / bitmap.PixelWidth;
                var scaleY = (double)height / bitmap.PixelHeight;
                
                var transform = new ScaleTransform(scaleX, scaleY);
                var resizedBitmap = new TransformedBitmap(bitmap, transform);
                resizedBitmap.Freeze();

                // Determine encoder based on file extension
                BitmapEncoder encoder = Path.GetExtension(destinationPath).ToLowerInvariant() switch
                {
                    ".png" => new PngBitmapEncoder(),
                    ".jpg" or ".jpeg" => new JpegBitmapEncoder { QualityLevel = 95 },
                    ".bmp" => new BmpBitmapEncoder(),
                    ".gif" => new GifBitmapEncoder(),
                    ".tiff" or ".tif" => new TiffBitmapEncoder(),
                    _ => new PngBitmapEncoder() // Default to PNG
                };

                encoder.Frames.Add(BitmapFrame.Create(resizedBitmap));

                // Save to file
                using var fileStream = new FileStream(destinationPath, FileMode.Create);
                encoder.Save(fileStream);

                _logger.LogInformation("Successfully resized image to {Destination}", destinationPath);
            }, ct);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resize image from {Source} to {Destination}", sourceImagePath, destinationPath);
            return false;
        }
    }
}
