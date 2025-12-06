using FileBoy.Core.Enums;
using FileBoy.Core.Models;

namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for managing file clipboard operations (cut/copy/paste).
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Copies file paths to clipboard.
    /// </summary>
    /// <param name="filePaths">Paths to copy.</param>
    void Copy(IEnumerable<string> filePaths);
    
    /// <summary>
    /// Cuts file paths to clipboard (for move operation).
    /// </summary>
    /// <param name="filePaths">Paths to cut.</param>
    void Cut(IEnumerable<string> filePaths);
    
    /// <summary>
    /// Gets current clipboard data.
    /// </summary>
    /// <returns>Clipboard data containing operation type and file paths.</returns>
    ClipboardData GetClipboardData();
    
    /// <summary>
    /// Clears the clipboard.
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Checks if there's file data in clipboard ready to paste.
    /// </summary>
    /// <returns>True if paste is available.</returns>
    bool CanPaste();
}
