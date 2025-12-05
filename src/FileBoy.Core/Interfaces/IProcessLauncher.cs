namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for launching external processes.
/// </summary>
public interface IProcessLauncher
{
    /// <summary>
    /// Opens a file with its associated application.
    /// </summary>
    /// <param name="filePath">Path to the file to open.</param>
    /// <returns>True if launch was successful.</returns>
    bool OpenWithDefault(string filePath);

    /// <summary>
    /// Opens a folder in Windows Explorer.
    /// </summary>
    /// <param name="folderPath">Path to the folder.</param>
    /// <returns>True if launch was successful.</returns>
    bool OpenInExplorer(string folderPath);

    /// <summary>
    /// Opens a folder in Explorer with a specific file selected.
    /// </summary>
    /// <param name="filePath">Path to the file to select.</param>
    /// <returns>True if launch was successful.</returns>
    bool ShowInExplorer(string filePath);
}
