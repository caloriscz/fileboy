using FileBoy.Core.Enums;

namespace FileBoy.Core.Extensions;

/// <summary>
/// Extension methods for file operations.
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Image extensions supported by the built-in viewer.
    /// </summary>
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".ico", ".tiff", ".tif"
    };

    /// <summary>
    /// Video extensions for thumbnail extraction.
    /// </summary>
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mkv", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".mpeg", ".mpg"
    };

    /// <summary>
    /// Determines the file item type based on extension.
    /// </summary>
    /// <param name="extension">File extension (with or without dot).</param>
    /// <returns>The file item type.</returns>
    public static FileItemType GetFileItemType(this string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return FileItemType.Other;

        var ext = extension.StartsWith('.') ? extension : $".{extension}";

        if (ImageExtensions.Contains(ext))
            return FileItemType.Image;

        if (VideoExtensions.Contains(ext))
            return FileItemType.Video;

        return FileItemType.Other;
    }

    /// <summary>
    /// Checks if the extension is a supported image format.
    /// </summary>
    /// <param name="extension">File extension to check.</param>
    /// <returns>True if the extension is a supported image.</returns>
    public static bool IsImageExtension(this string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;

        var ext = extension.StartsWith('.') ? extension : $".{extension}";
        return ImageExtensions.Contains(ext);
    }

    /// <summary>
    /// Checks if the extension is a supported video format.
    /// </summary>
    /// <param name="extension">File extension to check.</param>
    /// <returns>True if the extension is a video.</returns>
    public static bool IsVideoExtension(this string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;

        var ext = extension.StartsWith('.') ? extension : $".{extension}";
        return VideoExtensions.Contains(ext);
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    /// <param name="bytes">Size in bytes.</param>
    /// <returns>Formatted string (e.g., "1.5 MB").</returns>
    public static string FormatFileSize(this long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.##} {1}", len, sizes[order]);
    }
}
