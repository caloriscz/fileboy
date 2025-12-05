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
            new SettingItem("Appearance", "Thumbnail Size", "Size of thumbnails in pixels (50-300)", 
                SettingType.Slider, nameof(ThumbnailSize), minValue: 50, maxValue: 300),
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
    private int _thumbnailSize = 120;

    public Array StartupModeValues => Enum.GetValues(typeof(WindowStartupMode));

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
        ThumbnailSize = settings.ThumbnailSize;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var settings = _settingsService.Settings;
        settings.StartupMode = StartupMode;
        settings.ThumbnailSize = ThumbnailSize;
        await _settingsService.SaveAsync();
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        StartupMode = WindowStartupMode.Normal;
        ThumbnailSize = 120;
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
    Text
}
