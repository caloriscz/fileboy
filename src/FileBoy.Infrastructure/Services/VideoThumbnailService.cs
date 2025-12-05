using FileBoy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Stub implementation of video thumbnail service.
/// Full implementation requires FFmpeg integration.
/// </summary>
public sealed class VideoThumbnailService : IVideoThumbnailService
{
    private readonly IFFmpegManager _ffmpegManager;
    private readonly ILogger<VideoThumbnailService> _logger;

    public VideoThumbnailService(IFFmpegManager ffmpegManager, ILogger<VideoThumbnailService> logger)
    {
        _ffmpegManager = ffmpegManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsAvailable => _ffmpegManager.IsAvailable;

    /// <inheritdoc />
    public async Task<byte[]?> GetThumbnailAsync(string videoPath, int size = 120, CancellationToken ct = default)
    {
        if (!_ffmpegManager.IsAvailable)
        {
            _logger.LogDebug("FFmpeg not available, skipping video thumbnail for {Path}", videoPath);
            return null;
        }

        try
        {
            // TODO: Implement FFmpeg thumbnail extraction using FFMpegCore
            // This is a placeholder - full implementation will use:
            // await FFMpeg.SnapshotAsync(videoPath, outputPath, new Size(size, size), TimeSpan.Zero);

            _logger.LogDebug("Video thumbnail extraction not yet implemented for {Path}", videoPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract video thumbnail from {Path}", videoPath);
            return null;
        }
    }
}
