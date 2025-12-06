using FileBoy.Core.Models;

namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for file system operations.
/// </summary>
public interface IFileSystemService
{
    /// <summary>
    /// Gets all files and directories in the specified path.
    /// </summary>
    /// <param name="path">Directory path to list.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Collection of file items.</returns>
    Task<IEnumerable<FileItem>> GetItemsAsync(string path, CancellationToken ct = default);

    /// <summary>
    /// Gets detailed information about a single file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>File item details.</returns>
    Task<FileItem?> GetFileDetailsAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Checks if a path exists and is a directory.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if path is a valid directory.</returns>
    bool IsValidDirectory(string path);

    /// <summary>
    /// Gets available drives on the system.
    /// </summary>
    /// <returns>Collection of drive root paths.</returns>
    IEnumerable<string> GetDrives();

    /// <summary>
    /// Copies files and directories to a destination path.
    /// </summary>
    /// <param name="sourcePaths">Source file/directory paths.</param>
    /// <param name="destinationPath">Destination directory path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task CopyFilesAsync(IEnumerable<string> sourcePaths, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Moves files and directories to a destination path.
    /// </summary>
    /// <param name="sourcePaths">Source file/directory paths.</param>
    /// <param name="destinationPath">Destination directory path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task MoveFilesAsync(IEnumerable<string> sourcePaths, string destinationPath, CancellationToken ct = default);

    /// <summary>
    /// Deletes files and directories.
    /// </summary>
    /// <param name="paths">File/directory paths to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task DeleteFilesAsync(IEnumerable<string> paths, CancellationToken ct = default);

    /// <summary>
    /// Creates a new folder at the specified path.
    /// </summary>
    /// <param name="parentPath">Parent directory path.</param>
    /// <param name="folderName">Name of the new folder.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Full path of the created folder.</returns>
    Task<string> CreateFolderAsync(string parentPath, string folderName, CancellationToken ct = default);
}
