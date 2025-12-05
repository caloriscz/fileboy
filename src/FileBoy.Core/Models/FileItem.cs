using FileBoy.Core.Enums;

namespace FileBoy.Core.Models;

/// <summary>
/// Represents a file or directory in the file system.
/// </summary>
public sealed class FileItem
{
    /// <summary>
    /// Full path to the file or directory.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Name of the file or directory (without path).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// File extension (lowercase, with dot). Empty for directories.
    /// </summary>
    public string Extension { get; init; } = string.Empty;

    /// <summary>
    /// Type of the file item.
    /// </summary>
    public FileItemType ItemType { get; init; }

    /// <summary>
    /// File size in bytes. 0 for directories.
    /// </summary>
    public long Size { get; init; }

    /// <summary>
    /// Last modification date.
    /// </summary>
    public DateTime ModifiedDate { get; init; }

    /// <summary>
    /// Creation date.
    /// </summary>
    public DateTime CreatedDate { get; init; }

    /// <summary>
    /// Indicates if this is a directory.
    /// </summary>
    public bool IsDirectory => ItemType == FileItemType.Directory;

    /// <summary>
    /// Indicates if this file can be viewed in the built-in image viewer.
    /// </summary>
    public bool IsViewableImage => ItemType == FileItemType.Image;
}
