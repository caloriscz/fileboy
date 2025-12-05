using FileBoy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Manages FFmpeg binary availability and on-demand download.
/// </summary>
public sealed class FFmpegManager : IFFmpegManager
{
    private readonly ILogger<FFmpegManager> _logger;
    private readonly string _ffmpegDirectory;

    public FFmpegManager(ILogger<FFmpegManager> logger)
    {
        _logger = logger;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _ffmpegDirectory = Path.Combine(localAppData, "FileBoy", "ffmpeg");
    }

    /// <inheritdoc />
    public string FFmpegPath => Path.Combine(_ffmpegDirectory, "ffmpeg.exe");

    /// <inheritdoc />
    public string FFprobePath => Path.Combine(_ffmpegDirectory, "ffprobe.exe");

    /// <inheritdoc />
    public bool IsAvailable => File.Exists(FFmpegPath) && File.Exists(FFprobePath);

    /// <inheritdoc />
    public async Task<bool> EnsureAvailableAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (IsAvailable)
        {
            _logger.LogDebug("FFmpeg already available at {Path}", _ffmpegDirectory);
            return true;
        }

        _logger.LogInformation("FFmpeg not found, downloading...");

        try
        {
            Directory.CreateDirectory(_ffmpegDirectory);

            // TODO: Implement actual FFmpeg download
            // Options:
            // 1. Download from https://www.gyan.dev/ffmpeg/builds/
            // 2. Use FFMpegCore.Extensions.Downloader package
            // 3. Use embedded binaries from NuGet package

            // For now, log instructions for manual setup
            _logger.LogWarning(
                "FFmpeg auto-download not yet implemented. " +
                "Please download FFmpeg manually and place ffmpeg.exe and ffprobe.exe in: {Path}",
                _ffmpegDirectory);

            progress?.Report(100);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FFmpeg");
            return false;
        }
    }
}
