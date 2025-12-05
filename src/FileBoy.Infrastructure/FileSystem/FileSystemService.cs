using FileBoy.Core.Enums;
using FileBoy.Core.Extensions;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.FileSystem;

/// <summary>
/// File system service implementation.
/// </summary>
public sealed class FileSystemService : IFileSystemService
{
    private readonly ILogger<FileSystemService> _logger;

    public FileSystemService(ILogger<FileSystemService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FileItem>> GetItemsAsync(string path, CancellationToken ct = default)
    {
        _logger.LogInformation("Loading items from {Path}", path);

        return await Task.Run(() =>
        {
            var items = new List<FileItem>();

            try
            {
                var directoryInfo = new DirectoryInfo(path);

                if (!directoryInfo.Exists)
                {
                    _logger.LogWarning("Directory does not exist: {Path}", path);
                    return items;
                }

                // Add directories first
                foreach (var dir in directoryInfo.EnumerateDirectories())
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        items.Add(new FileItem
                        {
                            FullPath = dir.FullName,
                            Name = dir.Name,
                            ItemType = FileItemType.Directory,
                            ModifiedDate = dir.LastWriteTime,
                            CreatedDate = dir.CreationTime
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _logger.LogDebug("Access denied to directory: {Path}", dir.FullName);
                    }
                }

                // Add files
                foreach (var file in directoryInfo.EnumerateFiles())
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        items.Add(new FileItem
                        {
                            FullPath = file.FullName,
                            Name = file.Name,
                            Extension = file.Extension.ToLowerInvariant(),
                            ItemType = file.Extension.GetFileItemType(),
                            Size = file.Length,
                            ModifiedDate = file.LastWriteTime,
                            CreatedDate = file.CreationTime
                        });
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _logger.LogDebug("Access denied to file: {Path}", file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading items from {Path}", path);
            }

            return items;
        }, ct);
    }

    /// <inheritdoc />
    public async Task<FileItem?> GetFileDetailsAsync(string filePath, CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                var fileInfo = new FileInfo(filePath);

                if (!fileInfo.Exists)
                    return null;

                return new FileItem
                {
                    FullPath = fileInfo.FullName,
                    Name = fileInfo.Name,
                    Extension = fileInfo.Extension.ToLowerInvariant(),
                    ItemType = fileInfo.Extension.GetFileItemType(),
                    Size = fileInfo.Length,
                    ModifiedDate = fileInfo.LastWriteTime,
                    CreatedDate = fileInfo.CreationTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file details for {Path}", filePath);
                return null;
            }
        }, ct);
    }

    /// <inheritdoc />
    public bool IsValidDirectory(string path)
    {
        try
        {
            return Directory.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> GetDrives()
    {
        return DriveInfo.GetDrives()
            .Where(d => d.IsReady)
            .Select(d => d.RootDirectory.FullName);
    }
}
