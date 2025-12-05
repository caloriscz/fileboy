using FileBoy.Core.Models;

namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for loading and saving application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the current settings.
    /// </summary>
    AppSettings Settings { get; }

    /// <summary>
    /// Loads settings from storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task LoadAsync(CancellationToken ct = default);

    /// <summary>
    /// Saves current settings to storage.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task SaveAsync(CancellationToken ct = default);
}
