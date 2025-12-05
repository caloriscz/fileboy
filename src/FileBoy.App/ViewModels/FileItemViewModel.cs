using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using FileBoy.Core.Extensions;
using FileBoy.Core.Models;
using FileBoy.Core.Enums;

namespace FileBoy.App.ViewModels;

/// <summary>
/// ViewModel for a single file or directory item in the file list.
/// </summary>
public partial class FileItemViewModel : ObservableObject
{
    private readonly FileItem _model;

    public FileItemViewModel(FileItem model)
    {
        _model = model;
    }

    public FileItem Model => _model;
    public string Name => _model.Name;
    public string FullPath => _model.FullPath;
    public string Extension => _model.Extension;
    public long Size => _model.Size;
    public DateTime ModifiedDate => _model.ModifiedDate;
    public bool IsDirectory => _model.IsDirectory;
    public bool IsViewableImage => _model.IsViewableImage;
    public FileItemType ItemType => _model.ItemType;

    public string FormattedSize => _model.IsDirectory ? "" : _model.Size.FormatFileSize();

    public string TypeDescription => _model.ItemType switch
    {
        FileItemType.Directory => "Directory",
        FileItemType.Image => GetImageTypeDescription(_model.Extension),
        FileItemType.Video => GetVideoTypeDescription(_model.Extension),
        _ => string.IsNullOrEmpty(_model.Extension) 
            ? "File" 
            : $"{_model.Extension.TrimStart('.').ToUpperInvariant()} File"
    };

    public string IconGlyph => _model.ItemType switch
    {
        FileItemType.Directory => "📁",
        FileItemType.Image => "🖼",
        FileItemType.Video => "🎬",
        _ => "📄"
    };

    [ObservableProperty]
    private ImageSource? _thumbnail;

    private static string GetImageTypeDescription(string extension) => extension.ToLowerInvariant() switch
    {
        ".png" => "Portable Network Graphics",
        ".jpg" or ".jpeg" => "JPEG Image",
        ".gif" => "GIF Image",
        ".bmp" => "Bitmap Image",
        ".webp" => "WebP Image",
        ".ico" => "Icon",
        ".tiff" or ".tif" => "TIFF Image",
        _ => "Image"
    };

    private static string GetVideoTypeDescription(string extension) => extension.ToLowerInvariant() switch
    {
        ".mp4" => "MP4 Video",
        ".avi" => "AVI Video",
        ".mkv" => "MKV Video",
        ".mov" => "QuickTime Movie",
        ".wmv" => "Windows Media Video",
        ".webm" => "WebM Video",
        _ => "Video File"
    };
}
