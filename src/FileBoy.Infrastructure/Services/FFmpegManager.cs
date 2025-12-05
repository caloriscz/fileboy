using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
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
    private string? _resolvedFFmpegPath;
    private string? _resolvedFFprobePath;
    
    // FFmpeg essentials build from gyan.dev (smaller download ~30MB)
    private const string FFmpegDownloadUrl = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

    public FFmpegManager(ILogger<FFmpegManager> logger)
    {
        _logger = logger;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _ffmpegDirectory = Path.Combine(localAppData, "FileBoy", "ffmpeg");
        
        // Try to find FFmpeg on startup
        ResolveFFmpegPaths();
    }

    /// <inheritdoc />
    public string FFmpegPath => _resolvedFFmpegPath ?? Path.Combine(_ffmpegDirectory, "ffmpeg.exe");

    /// <inheritdoc />
    public string FFprobePath => _resolvedFFprobePath ?? Path.Combine(_ffmpegDirectory, "ffprobe.exe");

    /// <inheritdoc />
    public bool IsAvailable => !string.IsNullOrEmpty(_resolvedFFmpegPath) && File.Exists(_resolvedFFmpegPath);

    /// <summary>
    /// Tries to find FFmpeg in common locations and PATH.
    /// </summary>
    private void ResolveFFmpegPaths()
    {
        // 1. Check our local directory first
        var localFfmpeg = Path.Combine(_ffmpegDirectory, "ffmpeg.exe");
        var localFfprobe = Path.Combine(_ffmpegDirectory, "ffprobe.exe");
        
        if (File.Exists(localFfmpeg))
        {
            _resolvedFFmpegPath = localFfmpeg;
            _resolvedFFprobePath = localFfprobe;
            _logger.LogInformation("Found FFmpeg in local directory: {Path}", _ffmpegDirectory);
            return;
        }

        // 2. Check PATH environment variable
        var pathFFmpeg = FindInPath("ffmpeg.exe");
        if (pathFFmpeg != null)
        {
            _resolvedFFmpegPath = pathFFmpeg;
            _resolvedFFprobePath = FindInPath("ffprobe.exe") ?? Path.Combine(Path.GetDirectoryName(pathFFmpeg)!, "ffprobe.exe");
            _logger.LogInformation("Found FFmpeg in PATH: {Path}", pathFFmpeg);
            return;
        }

        // 3. Check common installation locations
        var commonPaths = new[]
        {
            @"C:\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files\ffmpeg\bin\ffmpeg.exe",
            @"C:\Program Files (x86)\ffmpeg\bin\ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Packages", "Gyan.FFmpeg_*", "ffmpeg-*-essentials_build", "bin", "ffmpeg.exe"),
        };

        foreach (var path in commonPaths)
        {
            if (path.Contains('*'))
            {
                // Handle wildcard paths (for winget installs)
                var dir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                if (dir != null && Directory.Exists(Path.GetDirectoryName(dir)))
                {
                    try
                    {
                        var matches = Directory.GetFiles(Path.GetDirectoryName(dir)!, "ffmpeg.exe", SearchOption.AllDirectories);
                        if (matches.Length > 0)
                        {
                            _resolvedFFmpegPath = matches[0];
                            _resolvedFFprobePath = Path.Combine(Path.GetDirectoryName(matches[0])!, "ffprobe.exe");
                            _logger.LogInformation("Found FFmpeg via wildcard search: {Path}", matches[0]);
                            return;
                        }
                    }
                    catch { }
                }
            }
            else if (File.Exists(path))
            {
                _resolvedFFmpegPath = path;
                _resolvedFFprobePath = Path.Combine(Path.GetDirectoryName(path)!, "ffprobe.exe");
                _logger.LogInformation("Found FFmpeg at common location: {Path}", path);
                return;
            }
        }

        _logger.LogDebug("FFmpeg not found in any known location");
    }

    private static string? FindInPath(string executable)
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;

        foreach (var path in pathEnv.Split(Path.PathSeparator))
        {
            try
            {
                var fullPath = Path.Combine(path.Trim(), executable);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            catch { }
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> EnsureAvailableAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        // Re-check in case it was installed since startup
        ResolveFFmpegPaths();
        
        if (IsAvailable)
        {
            _logger.LogDebug("FFmpeg already available at {Path}", FFmpegPath);
            progress?.Report(100);
            return true;
        }

        _logger.LogInformation("FFmpeg not found, downloading from {Url}...", FFmpegDownloadUrl);

        try
        {
            Directory.CreateDirectory(_ffmpegDirectory);
            
            var zipPath = Path.Combine(Path.GetTempPath(), $"ffmpeg_{Guid.NewGuid()}.zip");
            var extractPath = Path.Combine(Path.GetTempPath(), $"ffmpeg_extract_{Guid.NewGuid()}");

            try
            {
                // Download FFmpeg
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                
                progress?.Report(5);
                _logger.LogInformation("Downloading FFmpeg...");
                
                using var response = await httpClient.GetAsync(FFmpegDownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
                response.EnsureSuccessStatusCode();
                
                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var downloadedBytes = 0L;
                
                await using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
                await using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    
                    while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                    {
                        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                        downloadedBytes += bytesRead;
                        
                        if (totalBytes > 0)
                        {
                            var downloadProgress = (int)(5 + (downloadedBytes * 70 / totalBytes));
                            progress?.Report(downloadProgress);
                        }
                    }
                }
                
                _logger.LogInformation("Downloaded FFmpeg ({Size} MB), extracting...", downloadedBytes / 1024 / 1024);
                progress?.Report(75);
                
                // Extract ZIP
                ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
                progress?.Report(85);
                
                // Find ffmpeg.exe in extracted folder (it's in a subfolder)
                var ffmpegExe = Directory.GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                var ffprobeExe = Directory.GetFiles(extractPath, "ffprobe.exe", SearchOption.AllDirectories).FirstOrDefault();
                
                if (ffmpegExe == null || ffprobeExe == null)
                {
                    _logger.LogError("FFmpeg executables not found in downloaded archive");
                    return false;
                }
                
                // Copy to our directory
                File.Copy(ffmpegExe, Path.Combine(_ffmpegDirectory, "ffmpeg.exe"), overwrite: true);
                File.Copy(ffprobeExe, Path.Combine(_ffmpegDirectory, "ffprobe.exe"), overwrite: true);
                
                progress?.Report(95);
                
                // Re-resolve paths
                ResolveFFmpegPaths();
                
                _logger.LogInformation("FFmpeg installed successfully to {Path}", _ffmpegDirectory);
                progress?.Report(100);
                
                return IsAvailable;
            }
            finally
            {
                // Cleanup temp files
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                try { if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true); } catch { }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("FFmpeg download was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download FFmpeg");
            return false;
        }
    }
}
