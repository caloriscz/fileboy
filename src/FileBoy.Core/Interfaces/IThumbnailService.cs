using FileBoy.Core.Models;

namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for generating and caching thumbnails.
/// </summary>
public interface IThumbnailService
{
    /// <summary>
    /// Gets a thumbnail for a file item.
    /// </summary>
    /// <param name="item">File item to generate thumbnail for.</param>
    /// <param name="size">Desired thumbnail size in pixels (square).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Base64-encoded image data, or null if thumbnail cannot be generated.</returns>
    Task<string?> GetThumbnailAsync(FileItem item, int size = 120, CancellationToken ct = default);

    /// <summary>
    /// Preloads thumbnails for multiple items (for batch operations).
    /// </summary>
    /// <param name="items">File items to preload thumbnails for.</param>
    /// <param name="size">Desired thumbnail size in pixels.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PreloadThumbnailsAsync(IEnumerable<FileItem> items, int size = 120, CancellationToken ct = default);

    /// <summary>
    /// Clears the thumbnail cache.
    /// </summary>
    void ClearCache();
}
