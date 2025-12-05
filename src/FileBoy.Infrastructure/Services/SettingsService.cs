using System.Text.Json;
using FileBoy.Core.Interfaces;
using FileBoy.Core.Models;
using Microsoft.Extensions.Logging;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Settings service that persists to JSON file.
/// </summary>
public sealed class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;

    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var fileBoyPath = Path.Combine(appDataPath, "FileBoy");
        Directory.CreateDirectory(fileBoyPath);

        _settingsPath = Path.Combine(fileBoyPath, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public AppSettings Settings { get; private set; } = new();

    /// <inheritdoc />
    public async Task LoadAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                _logger.LogInformation("Loading settings from {Path}", _settingsPath);

                var json = await File.ReadAllTextAsync(_settingsPath, ct);
                Settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            else
            {
                _logger.LogInformation("Settings file not found, using defaults");
                Settings = new AppSettings();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            Settings = new AppSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Saving settings to {Path}", _settingsPath);

            var json = JsonSerializer.Serialize(Settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
        }
    }
}
