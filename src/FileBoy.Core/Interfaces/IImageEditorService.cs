using FileBoy.Core.Models;

namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for image editing operations.
/// </summary>
public interface IImageEditorService
{
    /// <summary>
    /// Crops an image to the specified rectangle.
    /// </summary>
    /// <param name="sourceImagePath">Path to the source image.</param>
    /// <param name="destinationPath">Path where cropped image will be saved.</param>
    /// <param name="cropRect">Rectangle defining the crop area (in pixels).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> CropImageAsync(string sourceImagePath, string destinationPath, CropRectangle cropRect, CancellationToken ct = default);

    /// <summary>
    /// Resizes an image to the specified dimensions.
    /// </summary>
    /// <param name="sourceImagePath">Path to the source image.</param>
    /// <param name="destinationPath">Path where resized image will be saved.</param>
    /// <param name="width">Target width in pixels.</param>
    /// <param name="height">Target height in pixels.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> ResizeImageAsync(string sourceImagePath, string destinationPath, int width, int height, CancellationToken ct = default);
}
