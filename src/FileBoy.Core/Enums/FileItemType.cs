namespace FileBoy.Core.Enums;

/// <summary>
/// Defines the type of file system item.
/// </summary>
public enum FileItemType
{
    /// <summary>
    /// A directory/folder.
    /// </summary>
    Directory,

    /// <summary>
    /// An image file that can be viewed in the built-in viewer.
    /// </summary>
    Image,

    /// <summary>
    /// A video file (thumbnail extracted, opens externally).
    /// </summary>
    Video,

    /// <summary>
    /// Any other file type (opens with associated program).
    /// </summary>
    Other
}
