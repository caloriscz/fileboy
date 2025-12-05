using FileBoy.Core.Interfaces;

namespace FileBoy.Infrastructure.Services;

/// <summary>
/// Browser-style navigation history implementation.
/// </summary>
public sealed class NavigationHistory : INavigationHistory
{
    private readonly List<string> _history = [];
    private int _currentIndex = -1;

    /// <inheritdoc />
    public string Current => _currentIndex >= 0 && _currentIndex < _history.Count
        ? _history[_currentIndex]
        : string.Empty;

    /// <inheritdoc />
    public bool CanGoBack => _currentIndex > 0;

    /// <inheritdoc />
    public bool CanGoForward => _currentIndex < _history.Count - 1;

    /// <inheritdoc />
    public IReadOnlyList<string> History => _history.AsReadOnly();

    /// <inheritdoc />
    public event EventHandler<string>? Navigated;

    /// <inheritdoc />
    public void Navigate(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        // If we're not at the end, remove forward history
        if (_currentIndex < _history.Count - 1)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        // Don't add duplicate consecutive entries
        if (_history.Count == 0 || !string.Equals(_history[^1], path, StringComparison.OrdinalIgnoreCase))
        {
            _history.Add(path);
        }

        _currentIndex = _history.Count - 1;
        Navigated?.Invoke(this, path);
    }

    /// <inheritdoc />
    public string GoBack()
    {
        if (!CanGoBack)
            return Current;

        _currentIndex--;
        Navigated?.Invoke(this, Current);
        return Current;
    }

    /// <inheritdoc />
    public string GoForward()
    {
        if (!CanGoForward)
            return Current;

        _currentIndex++;
        Navigated?.Invoke(this, Current);
        return Current;
    }
}
