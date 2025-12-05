using System.Windows.Controls;

namespace FileBoy.App.Services;

/// <summary>
/// Service for navigating between pages in the application.
/// </summary>
public interface IPageNavigationService
{
    void NavigateTo<TPage>() where TPage : Page;
    void NavigateTo(Page page);
    void GoBack();
    bool CanGoBack { get; }
}

/// <summary>
/// Implementation of page navigation using a WPF Frame.
/// </summary>
public class PageNavigationService : IPageNavigationService
{
    private Frame? _frame;

    public void SetFrame(Frame frame)
    {
        _frame = frame;
    }

    public void NavigateTo<TPage>() where TPage : Page
    {
        throw new NotImplementedException("Use NavigateTo(Page) instead");
    }

    public void NavigateTo(Page page)
    {
        _frame?.Navigate(page);
    }

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
        {
            _frame.GoBack();
        }
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;
}
