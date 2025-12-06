namespace FileBoy.Core.Enums;

/// <summary>
/// Clipboard operation type for file operations.
/// </summary>
public enum ClipboardOperation
{
    /// <summary>
    /// No operation.
    /// </summary>
    None,
    
    /// <summary>
    /// Copy operation (files will be duplicated).
    /// </summary>
    Copy,
    
    /// <summary>
    /// Cut operation (files will be moved).
    /// </summary>
    Cut
}
