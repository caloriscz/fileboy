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

    /// <inheritdoc />
    public async Task CopyFilesAsync(IEnumerable<string> sourcePaths, string destinationPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Copying {Count} items to {Destination}", sourcePaths.Count(), destinationPath);

        await Task.Run(() =>
        {
            foreach (var sourcePath in sourcePaths)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (File.Exists(sourcePath))
                    {
                        CopyFile(sourcePath, destinationPath, ct);
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        CopyDirectory(sourcePath, destinationPath, ct);
                    }
                    else
                    {
                        _logger.LogWarning("Source path does not exist: {Path}", sourcePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy {Source}", sourcePath);
                    throw;
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task MoveFilesAsync(IEnumerable<string> sourcePaths, string destinationPath, CancellationToken ct = default)
    {
        _logger.LogInformation("Moving {Count} items to {Destination}", sourcePaths.Count(), destinationPath);

        await Task.Run(() =>
        {
            foreach (var sourcePath in sourcePaths)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var name = Path.GetFileName(sourcePath);
                    var destPath = Path.Combine(destinationPath, name);

                    if (File.Exists(sourcePath))
                    {
                        // Handle file name conflicts
                        destPath = GetUniqueFilePath(destPath);
                        File.Move(sourcePath, destPath);
                        _logger.LogDebug("Moved file {Source} to {Dest}", sourcePath, destPath);
                    }
                    else if (Directory.Exists(sourcePath))
                    {
                        // Handle directory name conflicts
                        destPath = GetUniqueDirectoryPath(destPath);
                        Directory.Move(sourcePath, destPath);
                        _logger.LogDebug("Moved directory {Source} to {Dest}", sourcePath, destPath);
                    }
                    else
                    {
                        _logger.LogWarning("Source path does not exist: {Path}", sourcePath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to move {Source}", sourcePath);
                    throw;
                }
            }
        }, ct);
    }

    /// <inheritdoc />
    public async Task DeleteFilesAsync(IEnumerable<string> paths, CancellationToken ct = default)
    {
        _logger.LogInformation("Deleting {Count} items", paths.Count());

        await Task.Run(() =>
        {
            foreach (var path in paths)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        _logger.LogDebug("Deleted file {Path}", path);
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, recursive: true);
                        _logger.LogDebug("Deleted directory {Path}", path);
                    }
                    else
                    {
                        _logger.LogWarning("Path does not exist: {Path}", path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to delete {Path}", path);
                    throw;
                }
            }
        }, ct);
    }

    private void CopyFile(string sourcePath, string destinationPath, CancellationToken ct)
    {
        var fileName = Path.GetFileName(sourcePath);
        var destFilePath = Path.Combine(destinationPath, fileName);

        // Handle file name conflicts
        destFilePath = GetUniqueFilePath(destFilePath);

        File.Copy(sourcePath, destFilePath, overwrite: false);
        _logger.LogDebug("Copied file {Source} to {Dest}", sourcePath, destFilePath);
    }

    private void CopyDirectory(string sourcePath, string destinationPath, CancellationToken ct)
    {
        var dirName = Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar));
        var destDirPath = Path.Combine(destinationPath, dirName);

        // Handle directory name conflicts
        destDirPath = GetUniqueDirectoryPath(destDirPath);

        Directory.CreateDirectory(destDirPath);

        // Copy all files
        foreach (var file in Directory.GetFiles(sourcePath))
        {
            ct.ThrowIfCancellationRequested();
            var destFile = Path.Combine(destDirPath, Path.GetFileName(file));
            File.Copy(file, destFile);
        }

        // Copy all subdirectories
        foreach (var dir in Directory.GetDirectories(sourcePath))
        {
            ct.ThrowIfCancellationRequested();
            CopyDirectory(dir, destDirPath, ct);
        }

        _logger.LogDebug("Copied directory {Source} to {Dest}", sourcePath, destDirPath);
    }

    private static string GetUniqueFilePath(string filePath)
    {
        if (!File.Exists(filePath))
            return filePath;

        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);
        var counter = 1;

        while (File.Exists(filePath))
        {
            filePath = Path.Combine(directory, $"{fileNameWithoutExt} ({counter}){extension}");
            counter++;
        }

        return filePath;
    }

    private static string GetUniqueDirectoryPath(string dirPath)
    {
        if (!Directory.Exists(dirPath))
            return dirPath;

        var parentDir = Path.GetDirectoryName(dirPath) ?? string.Empty;
        var dirName = Path.GetFileName(dirPath.TrimEnd(Path.DirectorySeparatorChar));
        var counter = 1;

        while (Directory.Exists(dirPath))
        {
            dirPath = Path.Combine(parentDir, $"{dirName} ({counter})");
            counter++;
        }

        return dirPath;
    }
}
