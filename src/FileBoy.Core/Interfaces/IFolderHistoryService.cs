namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for tracking recently used folder destinations.
/// </summary>
public interface IFolderHistoryService
{
    /// <summary>
    /// Gets recently used folders, sorted from most recent to oldest.
    /// </summary>
    /// <returns>Collection of folder paths.</returns>
    IEnumerable<string> GetRecentFolders();

    /// <summary>
    /// Adds a folder to the recent history.
    /// </summary>
    /// <param name="folderPath">Folder path to add.</param>
    void AddFolder(string folderPath);

    /// <summary>
    /// Clears all folder history.
    /// </summary>
    void Clear();
}
