using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Thumbnail service with memory caching.
/// </summary>
public sealed class ThumbnailService : IThumbnailService
{
    private readonly IMemoryCache _cache;
    private readonly IVideoThumbnailService _videoThumbnailService;
    private readonly ILogger<ThumbnailService> _logger;

    public ThumbnailService(
        IMemoryCache cache,
        IVideoThumbnailService videoThumbnailService,
        ILogger<ThumbnailService> logger)
    {
        _cache = cache;
        _videoThumbnailService = videoThumbnailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetThumbnailAsync(FileItem item, int size = 120, CancellationToken ct = default)
    {
        var cacheKey = $"thumb:{item.FullPath}:{size}";

        if (_cache.TryGetValue(cacheKey, out string? cachedThumbnail))
        {
            return cachedThumbnail;
        }

        string? thumbnail = null;

        try
        {
            thumbnail = item.ItemType switch
            {
                FileItemType.Image => await GenerateImageThumbnailAsync(item.FullPath, size, ct),
                FileItemType.Video => await GenerateVideoThumbnailAsync(item.FullPath, size, ct),
                _ => null
            };

            if (thumbnail != null)
            {
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSize(1)
                    .SetSlidingExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(cacheKey, thumbnail, cacheOptions);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {Path}", item.FullPath);
        }

        return thumbnail;
    }

    /// <inheritdoc />
    public async Task PreloadThumbnailsAsync(IEnumerable<FileItem> items, int size = 120, CancellationToken ct = default)
    {
        var tasks = items
            .Where(i => i.ItemType is FileItemType.Image or FileItemType.Video)
            .Select(item => GetThumbnailAsync(item, size, ct));

        await Task.WhenAll(tasks);
    }

    /// <inheritdoc />
    public void ClearCache()
    {
        if (_cache is MemoryCache mc)
        {
            mc.Compact(1.0);
        }
    }

    private async Task<string?> GenerateImageThumbnailAsync(string imagePath, int size, CancellationToken ct)
    {
        return await Task.Run(() =>
        {
            try
            {
                // Read image bytes and return as base64
                // For MVP, we'll return the original image and let the browser resize
                // In production, use SkiaSharp or ImageSharp for proper thumbnail generation
                var bytes = File.ReadAllBytes(imagePath);
                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                var mimeType = GetMimeType(extension);

                return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read image: {Path}", imagePath);
                return null;
            }
        }, ct);
    }

    private async Task<string?> GenerateVideoThumbnailAsync(string videoPath, int size, CancellationToken ct)
    {
        if (!_videoThumbnailService.IsAvailable)
        {
            _logger.LogDebug("Video thumbnail service not available");
            return null;
        }

        var bytes = await _videoThumbnailService.GetThumbnailAsync(videoPath, size, ct);

        if (bytes == null)
            return null;

        return $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
    }

    private static string GetMimeType(string extension) => extension switch
    {
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".gif" => "image/gif",
        ".bmp" => "image/bmp",
        ".webp" => "image/webp",
        ".ico" => "image/x-icon",
        ".tiff" or ".tif" => "image/tiff",
        _ => "application/octet-stream"
    };
}
