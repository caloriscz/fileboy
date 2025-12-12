using FileBoy.Core.Enums;

namespace FileBoy.Core.Models;

/// <summary>
/// Application settings stored in JSON.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Window startup state (Normal, Minimized, Maximized).
    /// </summary>
    public WindowStartupMode StartupMode { get; set; } = WindowStartupMode.Normal;

    /// <summary>
    /// Default view mode when opening folders.
    /// </summary>
    public ViewMode DefaultView { get; set; } = ViewMode.List;

    /// <summary>
    /// Last browsed path (restored on startup).
    /// </summary>
    public string LastPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

    /// <summary>
    /// Navigation history for back/forward.
    /// </summary>
    public List<string> PathHistory { get; set; } = [];

    /// <summary>
    /// Current position in path history.
    /// </summary>
    public int HistoryIndex { get; set; } = -1;

    /// <summary>
    /// Maximum number of thumbnails to keep in memory cache.
    /// </summary>
    public int ThumbnailCacheSize { get; set; } = 500;

    /// <summary>
    /// Thumbnail size in pixels (square).
    /// </summary>
    public int ThumbnailSize { get; set; } = 120;

    /// <summary>
    /// Preview panel width in pixels.
    /// </summary>
    public double PreviewPanelWidth { get; set; } = 400;

    /// <summary>
    /// Image display mode in detail viewer (Original, FitToScreen, FitIfLarger).
    /// </summary>
    public ImageDisplayMode ImageDisplayMode { get; set; } = ImageDisplayMode.FitIfLarger;

    /// <summary>
    /// Video seek interval in seconds (Left/Right arrow keys).
    /// </summary>
    public int VideoSeekInterval { get; set; } = 5;

    /// <summary>
    /// Folder where video snapshots are saved.
    /// </summary>
    public string SnapshotFolder { get; set; } = string.Empty;

    /// <summary>
    /// Filename template for snapshots. Available placeholders: {name}, {counter}, {timestamp}.
    /// </summary>
    public string SnapshotNameTemplate { get; set; } = "{name}_snapshot_{counter}";

    /// <summary>
    /// Recently used folders for copy/move operations (most recent first).
    /// </summary>
    public List<string> RecentFolders { get; set; } = [];
}
