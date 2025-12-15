using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileBoy.Core.Enums;
using FileBoy.Core.Interfaces;

namespace FileBoy.App.ViewModels;

/// <summary>
/// ViewModel for the Settings window.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        
        // Initialize categories
        Categories =
        [
            new SettingsCategory("General", "⚙", ["startup", "window", "general"]),
            new SettingsCategory("Appearance", "🎨", ["thumbnail", "size", "view", "appearance"]),
        ];
        
        AllSettings =
        [
            new SettingItem("General", "Window Startup Mode", "Choose how the window appears when the application starts", 
                SettingType.Enum, nameof(StartupMode), typeof(WindowStartupMode)),
            new SettingItem("General", "Default View Mode", "Choose whether to start in List or Thumbnail view", 
                SettingType.Enum, nameof(DefaultViewMode), typeof(ViewMode)),
            new SettingItem("Appearance", "Thumbnail Size", "Size of thumbnails in pixels (50-600)", 
                SettingType.Slider, nameof(ThumbnailSize), minValue: 50, maxValue: 600),
            new SettingItem("Appearance", "Image Display Mode", "How images are displayed in the viewer (Original size, Fit to screen, or Fit if larger)", 
                SettingType.Enum, nameof(ImageDisplayMode), typeof(MediaDisplayMode)),
            new SettingItem("Appearance", "Video Display Mode", "How videos are displayed in the viewer (Original size, Fit to screen, or Fit if larger)", 
                SettingType.Enum, nameof(VideoDisplayMode), typeof(MediaDisplayMode)),
            new SettingItem("General", "Video Seek Interval", "Seconds to skip when using Left/Right arrow keys in video player", 
                SettingType.Enum, nameof(VideoSeekInterval), typeof(int)),
            new SettingItem("General", "Loop Video", "Automatically restart video when it ends", 
                SettingType.Boolean, nameof(LoopVideo)),
            new SettingItem("General", "Snapshot Folder", "Folder where video snapshots are saved", 
                SettingType.FolderPicker, nameof(SnapshotFolder)),
            new SettingItem("General", "Snapshot Filename Template", "Template for snapshot filenames. Use {name} for video name, {counter} for number, {timestamp} for date/time", 
                SettingType.Text, nameof(SnapshotNameTemplate)),
        ];
        
        FilteredSettings = new ObservableCollection<SettingItem>(AllSettings);
        SelectedCategory = Categories[0];
        
        LoadSettings();
    }

    [ObservableProperty]
    private ObservableCollection<SettingsCategory> _categories;

    [ObservableProperty]
    private SettingsCategory? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SettingItem> _filteredSettings;

    public List<SettingItem> AllSettings { get; }

    // Setting values
    [ObservableProperty]
    private WindowStartupMode _startupMode;

    [ObservableProperty]
    private ViewMode _defaultViewMode;

    [ObservableProperty]
    private int _thumbnailSize = 120;

    [ObservableProperty]
    private MediaDisplayMode _imageDisplayMode;

    [ObservableProperty]
    private MediaDisplayMode _videoDisplayMode;

    [ObservableProperty]
    private int _videoSeekInterval;

    [ObservableProperty]
    private bool _loopVideo;

    [ObservableProperty]
    private string _snapshotFolder = string.Empty;

    [ObservableProperty]
    private string _snapshotNameTemplate = string.Empty;

    public Array StartupModeValues => Enum.GetValues(typeof(WindowStartupMode));
    public Array DefaultViewModeValues => Enum.GetValues(typeof(ViewMode));
    public Array MediaDisplayModeValues => Enum.GetValues(typeof(MediaDisplayMode));
    public int[] VideoSeekIntervalValues => [5, 10, 15, 20, 30, 45, 60];

    partial void OnSelectedCategoryChanged(SettingsCategory? value)
    {
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchText?.ToLowerInvariant() ?? string.Empty;
        var category = SelectedCategory?.Name;

        var filtered = AllSettings.Where(s =>
        {
            // Category filter
            var matchesCategory = string.IsNullOrEmpty(category) || 
                                  category == "All" || 
                                  s.Category == category;

            // Search filter
            var matchesSearch = string.IsNullOrEmpty(search) ||
                               s.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               s.Description.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               s.Category.Contains(search, StringComparison.OrdinalIgnoreCase);

            return matchesCategory && matchesSearch;
        });

        FilteredSettings = new ObservableCollection<SettingItem>(filtered);
    }

    private void LoadSettings()
    {
        var settings = _settingsService.Settings;
        StartupMode = settings.StartupMode;
        DefaultViewMode = settings.DefaultView;
        ThumbnailSize = settings.ThumbnailSize;
        ImageDisplayMode = settings.ImageDisplayMode;
        VideoDisplayMode = settings.VideoDisplayMode;
        VideoSeekInterval = settings.VideoSeekInterval;
        LoopVideo = settings.LoopVideo;
        SnapshotFolder = settings.SnapshotFolder;
        SnapshotNameTemplate = settings.SnapshotNameTemplate;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = _settingsService.Settings;
        settings.StartupMode = StartupMode;
        settings.DefaultView = DefaultViewMode;
        settings.ThumbnailSize = ThumbnailSize;
        settings.ImageDisplayMode = ImageDisplayMode;
        settings.VideoDisplayMode = VideoDisplayMode;
        settings.VideoSeekInterval = VideoSeekInterval;
        settings.LoopVideo = LoopVideo;
        settings.SnapshotFolder = SnapshotFolder;
        settings.SnapshotNameTemplate = SnapshotNameTemplate;
        await _settingsService.SaveAsync();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        StartupMode = WindowStartupMode.Normal;
        DefaultViewMode = ViewMode.List;
        ThumbnailSize = 120;
        ImageDisplayMode = MediaDisplayMode.FitIfLarger;
        VideoDisplayMode = MediaDisplayMode.FitToScreen;
        VideoSeekInterval = 5;
        LoopVideo = false;
        SnapshotFolder = string.Empty;
        SnapshotNameTemplate = "{name}_snapshot_{counter}";
    }
}

/// <summary>
/// Represents a settings category in the sidebar.
/// </summary>
public class SettingsCategory
{
    public string Name { get; }
    public string Icon { get; }
    public string[] SearchKeywords { get; }

    public SettingsCategory(string name, string icon, string[] searchKeywords)
    {
        Name = name;
        Icon = icon;
        SearchKeywords = searchKeywords;
    }
}

/// <summary>
/// Represents a single setting item.
/// </summary>
public class SettingItem
{
    public string Category { get; }
    public string Name { get; }
    public string Description { get; }
    public SettingType Type { get; }
    public string PropertyName { get; }
    public Type? EnumType { get; }
    public int MinValue { get; }
    public int MaxValue { get; }

    public SettingItem(string category, string name, string description, SettingType type, string propertyName, 
        Type? enumType = null, int minValue = 0, int maxValue = 100)
    {
        Category = category;
        Name = name;
        Description = description;
        Type = type;
        PropertyName = propertyName;
        EnumType = enumType;
        MinValue = minValue;
        MaxValue = maxValue;
    }
}

public enum SettingType
{
    Boolean,
    Enum,
    Slider,
    Text,
    FolderPicker
}
