using FileBoy.Core.Enums;

namespace FileBoy.Core.Models;

/// <summary>
/// Represents clipboard data for file operations.
/// </summary>
public sealed class ClipboardData
{
    /// <summary>
    /// Gets or sets the operation type (Copy or Cut).
    /// </summary>
    public ClipboardOperation Operation { get; set; }
    
    /// <summary>
    /// Gets or sets the list of file paths in the clipboard.
    /// </summary>
    public List<string> FilePaths { get; set; } = [];
    
    /// <summary>
    /// Gets whether the clipboard has any data.
    /// </summary>
    public bool HasData => FilePaths.Count > 0 && Operation != ClipboardOperation.None;
}
