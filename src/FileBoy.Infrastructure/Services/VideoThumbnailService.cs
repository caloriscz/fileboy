using System.Diagnostics;
using FileBoy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Service for extracting thumbnails from video files using FFmpeg.
/// </summary>
public sealed class VideoThumbnailService : IVideoThumbnailService
{
    private readonly IFFmpegManager _ffmpegManager;
    private readonly ILogger<VideoThumbnailService> _logger;
    
    /// <summary>
    /// Common video extensions supported for thumbnail extraction.
    /// </summary>
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".webm", ".m4v", ".flv", ".mpg", ".mpeg", ".3gp"
    };

    public VideoThumbnailService(IFFmpegManager ffmpegManager, ILogger<VideoThumbnailService> logger)
    {
        _ffmpegManager = ffmpegManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsAvailable => _ffmpegManager.IsAvailable;

    /// <summary>
    /// Checks if the given file extension is a supported video format.
    /// </summary>
    public static bool IsSupportedVideo(string extension) => SupportedExtensions.Contains(extension);

    /// <inheritdoc />
    public async Task<byte[]?> GetThumbnailAsync(string videoPath, int size = 120, CancellationToken ct = default)
    {
        if (!_ffmpegManager.IsAvailable)
        {
            _logger.LogDebug("FFmpeg not available, skipping video thumbnail for {Path}", videoPath);
            return null;
        }

        if (!File.Exists(videoPath))
        {
            _logger.LogWarning("Video file not found: {Path}", videoPath);
            return null;
        }

        var extension = Path.GetExtension(videoPath);
        if (!IsSupportedVideo(extension))
        {
            _logger.LogDebug("Unsupported video format: {Extension}", extension);
            return null;
        }

        try
        {
            // Create temp file for the thumbnail
            var tempFile = Path.Combine(Path.GetTempPath(), $"fileboy_thumb_{Guid.NewGuid()}.jpg");
            
            try
            {
                // FFmpeg command to extract frame as thumbnail
                // -ss BEFORE -i enables fast seeking (input seeking) - crucial for large files!
                // -ss 00:00:01 seeks to 1 second (to skip black frames at start)
                // -vframes 1 extracts only 1 frame
                // -vf scale scales to specified size maintaining aspect ratio
                // -q:v 2 sets JPEG quality (2 = high quality)
                var arguments = $"-ss 00:00:01 -i \"{videoPath}\" -vframes 1 -vf \"scale={size}:-1\" -q:v 2 -y \"{tempFile}\"";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegManager.FFmpegPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = new Process { StartInfo = startInfo };
                process.Start();
                
                // Consume stderr to prevent buffer deadlock
                _ = process.StandardError.ReadToEndAsync();
                
                // Wait for FFmpeg to complete (30 second timeout for larger files)
                var completed = await Task.Run(() => process.WaitForExit(30000), ct);
                
                if (!completed)
                {
                    _logger.LogWarning("FFmpeg timeout for {Path}", videoPath);
                    try { process.Kill(); } catch { }
                    return null;
                }

                if (ct.IsCancellationRequested)
                {
                    return null;
                }

                // Read the generated thumbnail
                if (File.Exists(tempFile))
                {
                    var thumbnailData = await File.ReadAllBytesAsync(tempFile, ct);
                    _logger.LogDebug("Generated video thumbnail for {Path}, size: {Size} bytes", videoPath, thumbnailData.Length);
                    return thumbnailData;
                }
                else
                {
                    // Try without seeking (for very short videos less than 1 second)
                    _logger.LogDebug("Retrying thumbnail without seek for {Path}", videoPath);
                    arguments = $"-i \"{videoPath}\" -vframes 1 -vf \"scale={size}:-1\" -q:v 2 -y \"{tempFile}\"";
                    startInfo.Arguments = arguments;
                    
                    using var retryProcess = new Process { StartInfo = startInfo };
                    retryProcess.Start();
                    _ = retryProcess.StandardError.ReadToEndAsync();
                    await Task.Run(() => retryProcess.WaitForExit(30000), ct);
                    
                    if (File.Exists(tempFile))
                    {
                        var thumbnailData = await File.ReadAllBytesAsync(tempFile, ct);
                        _logger.LogDebug("Generated video thumbnail (no seek) for {Path}, size: {Size} bytes", videoPath, thumbnailData.Length);
                        return thumbnailData;
                    }
                    
                    _logger.LogWarning("FFmpeg did not produce output for {Path}", videoPath);
                    return null;
                }
            }
            finally
            {
                // Clean up temp file
                try
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                catch { }
            }
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract video thumbnail from {Path}", videoPath);
            return null;
        }
    }
}
