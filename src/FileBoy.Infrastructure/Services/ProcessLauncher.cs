using System.Diagnostics;
using FileBoy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Launches external processes and applications.
/// </summary>
public sealed class ProcessLauncher : IProcessLauncher
{
    private readonly ILogger<ProcessLauncher> _logger;

    public ProcessLauncher(ILogger<ProcessLauncher> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public bool OpenWithDefault(string filePath)
    {
        try
        {
            _logger.LogInformation("Opening file with default application: {Path}", filePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file: {Path}", filePath);
            return false;
        }
    }

    /// <inheritdoc />
    public bool OpenInExplorer(string folderPath)
    {
        try
        {
            _logger.LogInformation("Opening folder in Explorer: {Path}", folderPath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folderPath}\"",
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open folder: {Path}", folderPath);
            return false;
        }
    }

    /// <inheritdoc />
    public bool ShowInExplorer(string filePath)
    {
        try
        {
            _logger.LogInformation("Showing file in Explorer: {Path}", filePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show file in Explorer: {Path}", filePath);
            return false;
        }
    }
}
