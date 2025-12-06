using System.Collections.Specialized;
using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Implementation of clipboard service for file operations.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    private readonly ILogger<ClipboardService> _logger;
    private ClipboardData _clipboardData = new();

    public ClipboardService(ILogger<ClipboardService> logger)
    {
        _logger = logger;
    }

    public void Copy(IEnumerable<string> filePaths)
    {
        var paths = filePaths.ToList();
        if (paths.Count == 0)
        {
            _logger.LogWarning("Attempted to copy with no file paths");
            return;
        }

        try
        {
            _clipboardData = new ClipboardData
            {
                Operation = ClipboardOperation.Copy,
                FilePaths = paths
            };

            _logger.LogInformation("Copied {Count} items to clipboard", paths.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy to clipboard");
            throw;
        }
    }

    public void Cut(IEnumerable<string> filePaths)
    {
        var paths = filePaths.ToList();
        if (paths.Count == 0)
        {
            _logger.LogWarning("Attempted to cut with no file paths");
            return;
        }

        try
        {
            _clipboardData = new ClipboardData
            {
                Operation = ClipboardOperation.Cut,
                FilePaths = paths
            };

            _logger.LogInformation("Cut {Count} items to clipboard", paths.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cut to clipboard");
            throw;
        }
    }

    public ClipboardData GetClipboardData()
    {
        return _clipboardData;
    }

    public void Clear()
    {
        _clipboardData = new ClipboardData();
        _logger.LogDebug("Cleared clipboard");
    }

    public bool CanPaste()
    {
        return _clipboardData.HasData;
    }
}
