namespace FileBoy.Core.Enums;

/// <summary>
/// Media (image/video) display mode in detail viewer.
/// </summary>
public enum MediaDisplayMode
{
    /// <summary>
    /// Display media at its original size (100%).
    /// </summary>
    Original,
    
    /// <summary>
    /// Always fit media to screen dimensions.
    /// </summary>
    FitToScreen,
    
    /// <summary>
    /// Fit to screen only if media is larger than screen.
    /// </summary>
    FitIfLarger
}
