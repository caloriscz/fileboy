namespace FileBoy.Core.Enums;

/// <summary>
/// Image display mode in detail viewer.
/// </summary>
public enum ImageDisplayMode
{
    /// <summary>
    /// Display image at its original size (100%).
    /// </summary>
    Original,
    
    /// <summary>
    /// Always fit image to screen dimensions.
    /// </summary>
    FitToScreen,
    
    /// <summary>
    /// Fit to screen only if image is larger than screen.
    /// </summary>
    FitIfLarger
}
