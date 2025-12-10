using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Service for tracking recently used folder destinations.
/// </summary>
public sealed class FolderHistoryService : IFolderHistoryService
{
    private const int MaxRecentFolders = 20;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<FolderHistoryService> _logger;

    public FolderHistoryService(ISettingsService settingsService, ILogger<FolderHistoryService> logger)
    {
        _settingsService = settingsService;
        _logger = logger;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetRecentFolders()
    {
        var recentFolders = _settingsService.Settings.RecentFolders ?? [];
        return recentFolders;
    }

    /// <inheritdoc />
    public void AddFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return;
        }

        var recentFolders = _settingsService.Settings.RecentFolders ?? [];

        // Remove if already exists (to move to top)
        recentFolders.RemoveAll(f => string.Equals(f, folderPath, StringComparison.OrdinalIgnoreCase));

        // Add to top
        recentFolders.Insert(0, folderPath);

        // Trim to max size
        if (recentFolders.Count > MaxRecentFolders)
        {
            recentFolders = recentFolders.Take(MaxRecentFolders).ToList();
        }

        _settingsService.Settings.RecentFolders = recentFolders;
        _ = _settingsService.SaveAsync();

        _logger.LogDebug("Added folder to recent history: {FolderPath}", folderPath);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _settingsService.Settings.RecentFolders = [];
        _ = _settingsService.SaveAsync();

        _logger.LogInformation("Cleared folder history");
    }
}
