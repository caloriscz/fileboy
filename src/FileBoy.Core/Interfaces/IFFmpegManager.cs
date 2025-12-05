namespace FileBoy.Core.Interfaces;

/// <summary>
/// Manages FFmpeg binary availability and download.
/// </summary>
public interface IFFmpegManager
{
    /// <summary>
    /// Path to the FFmpeg executable.
    /// </summary>
    string FFmpegPath { get; }

    /// <summary>
    /// Path to the FFprobe executable.
    /// </summary>
    string FFprobePath { get; }

    /// <summary>
    /// Indicates if FFmpeg is ready to use.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Ensures FFmpeg binaries are available, downloading if necessary.
    /// </summary>
    /// <param name="progress">Optional progress callback (0-100).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if FFmpeg is ready, false if download failed.</returns>
    Task<bool> EnsureAvailableAsync(IProgress<int>? progress = null, CancellationToken ct = default);
}
