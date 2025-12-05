namespace FileBoy.Core.Interfaces;

/// <summary>
/// Service for managing browser-style navigation history.
/// </summary>
public interface INavigationHistory
{
    /// <summary>
    /// Current path being displayed.
    /// </summary>
    string Current { get; }

    /// <summary>
    /// Indicates if back navigation is available.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Indicates if forward navigation is available.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Navigates to a new path, clearing forward history.
    /// </summary>
    /// <param name="path">Path to navigate to.</param>
    void Navigate(string path);

    /// <summary>
    /// Goes back to the previous path.
    /// </summary>
    /// <returns>The previous path, or current if cannot go back.</returns>
    string GoBack();

    /// <summary>
    /// Goes forward to the next path.
    /// </summary>
    /// <returns>The next path, or current if cannot go forward.</returns>
    string GoForward();

    /// <summary>
    /// Gets the full history list.
    /// </summary>
    IReadOnlyList<string> History { get; }

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<string>? Navigated;
}
