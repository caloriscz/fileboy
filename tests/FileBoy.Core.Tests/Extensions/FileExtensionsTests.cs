using FileBoy.Core.Enums;
using FileBoy.Core.Extensions;

namespace FileBoy.Core.Tests.Extensions;

public class FileExtensionsTests
{
    [Theory]
    [InlineData(".jpg", FileItemType.Image)]
    [InlineData(".jpeg", FileItemType.Image)]
    [InlineData(".png", FileItemType.Image)]
    [InlineData(".gif", FileItemType.Image)]
    [InlineData(".bmp", FileItemType.Image)]
    [InlineData(".webp", FileItemType.Image)]
    [InlineData(".ico", FileItemType.Image)]
    [InlineData(".tiff", FileItemType.Image)]
    [InlineData(".tif", FileItemType.Image)]
    [InlineData(".JPG", FileItemType.Image)]
    [InlineData(".PNG", FileItemType.Image)]
    public void GetFileItemType_ImageExtensions_ReturnsImage(string extension, FileItemType expected)
    {
        // Arrange & Act
        var result = extension.GetFileItemType();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".mp4", FileItemType.Video)]
    [InlineData(".avi", FileItemType.Video)]
    [InlineData(".mkv", FileItemType.Video)]
    [InlineData(".mov", FileItemType.Video)]
    [InlineData(".wmv", FileItemType.Video)]
    [InlineData(".MP4", FileItemType.Video)]
    public void GetFileItemType_VideoExtensions_ReturnsVideo(string extension, FileItemType expected)
    {
        // Arrange & Act
        var result = extension.GetFileItemType();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".txt", FileItemType.Other)]
    [InlineData(".pdf", FileItemType.Other)]
    [InlineData(".exe", FileItemType.Other)]
    [InlineData("", FileItemType.Other)]
    public void GetFileItemType_OtherExtensions_ReturnsOther(string extension, FileItemType expected)
    {
        // Arrange & Act
        var result = extension.GetFileItemType();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".jpg", true)]
    [InlineData(".png", true)]
    [InlineData(".mp4", false)]
    [InlineData(".txt", false)]
    [InlineData("", false)]
    public void IsImageExtension_ReturnsCorrectValue(string extension, bool expected)
    {
        // Arrange & Act
        var result = extension.IsImageExtension();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(".mp4", true)]
    [InlineData(".avi", true)]
    [InlineData(".jpg", false)]
    [InlineData(".txt", false)]
    [InlineData("", false)]
    public void IsVideoExtension_ReturnsCorrectValue(string extension, bool expected)
    {
        // Arrange & Act
        var result = extension.IsVideoExtension();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(512, "512 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatFileSize_ReturnsCorrectFormat(long bytes, string expected)
    {
        // Arrange & Act
        var result = bytes.FormatFileSize();

        // Assert
        Assert.Equal(expected, result);
    }
}
