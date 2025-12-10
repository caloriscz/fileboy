using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace FileBoy.App.ViewModels;

/// <summary>
/// ViewModel for the Copy/Move to Folder dialog.
/// </summary>
public partial class CopyMoveToFolderViewModel : ObservableObject
{
    private readonly IFolderHistoryService _folderHistoryService;
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<CopyMoveToFolderViewModel> _logger;

    public CopyMoveToFolderViewModel(
        IFolderHistoryService folderHistoryService,
        IFileSystemService fileSystemService,
        ILogger<CopyMoveToFolderViewModel> logger)
    {
        _folderHistoryService = folderHistoryService;
        _fileSystemService = fileSystemService;
        _logger = logger;

        RecentFolders = [];
        LoadRecentFolders();
    }

    [ObservableProperty]
    private CopyMoveOperation _operation = CopyMoveOperation.Copy;

    [ObservableProperty]
    private string _destinationFolder = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _recentFolders;

    /// <summary>
    /// Gets whether there are any recent folders available.
    /// </summary>
    public bool HasRecentFolders => RecentFolders.Count > 0;

    [ObservableProperty]
    private string? _selectedRecentFolder;

    partial void OnSelectedRecentFolderChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            DestinationFolder = value;
        }
    }

    /// <summary>
    /// Gets whether the current destination is valid.
    /// </summary>
    public bool IsDestinationValid => 
        !string.IsNullOrWhiteSpace(DestinationFolder) && 
        _fileSystemService.IsValidDirectory(DestinationFolder);

    /// <summary>
    /// Loads recent folders from history.
    /// </summary>
    private void LoadRecentFolders()
    {
        RecentFolders.Clear();
        foreach (var folder in _folderHistoryService.GetRecentFolders())
        {
            if (_fileSystemService.IsValidDirectory(folder))
            {
                RecentFolders.Add(folder);
            }
        }

        OnPropertyChanged(nameof(HasRecentFolders));
        _logger.LogDebug("Loaded {Count} recent folders", RecentFolders.Count);
    }

    /// <summary>
    /// Opens folder browser dialog to select destination.
    /// </summary>
    [RelayCommand]
    private void BrowseFolder()
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select destination folder",
            ShowNewFolderButton = true,
            UseDescriptionForTitle = true
        };

        if (!string.IsNullOrWhiteSpace(DestinationFolder) && 
            _fileSystemService.IsValidDirectory(DestinationFolder))
        {
            dialog.SelectedPath = DestinationFolder;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DestinationFolder = dialog.SelectedPath;
            OnPropertyChanged(nameof(IsDestinationValid));
            _logger.LogDebug("Selected folder: {Folder}", DestinationFolder);
        }
    }

    /// <summary>
    /// Resets the dialog to initial state.
    /// </summary>
    public void Reset()
    {
        Operation = CopyMoveOperation.Copy;
        DestinationFolder = string.Empty;
        SelectedRecentFolder = null;
        LoadRecentFolders();
    }

    /// <summary>
    /// Saves the destination folder to history.
    /// </summary>
    public void SaveToHistory()
    {
        if (IsDestinationValid)
        {
            _folderHistoryService.AddFolder(DestinationFolder);
        }
    }
}
