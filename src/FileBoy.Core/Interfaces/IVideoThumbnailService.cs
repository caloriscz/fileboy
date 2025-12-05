namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for extracting thumbnails from video files.
/// </summary>
public interface IVideoThumbnailService
{
    /// <summary>
    /// Extracts a thumbnail from a video file (first frame).
    /// </summary>
    /// <param name="videoPath">Path to the video file.</param>
    /// <param name="size">Desired thumbnail size in pixels.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Thumbnail image data as bytes, or null if extraction fails.</returns>
    Task<byte[]?> GetThumbnailAsync(string videoPath, int size = 120, CancellationToken ct = default);

    /// <summary>
    /// Checks if video thumbnail extraction is available (FFmpeg ready).
    /// </summary>
    bool IsAvailable { get; }
}
