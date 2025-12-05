using FileBoy.Infrastructure.Services;

namespace FileBoy.Infrastructure.Tests.Services;

public class NavigationHistoryTests
{
    [Fact]
    public void Navigate_FirstNavigation_SetsCurrent()
    {
        // Arrange
        var history = new NavigationHistory();

        // Act
        history.Navigate(@"C:\Users");

        // Assert
        Assert.Equal(@"C:\Users", history.Current);
    }

    [Fact]
    public void Navigate_MultipleNavigations_UpdatesHistory()
    {
        // Arrange
        var history = new NavigationHistory();

        // Act
        history.Navigate(@"C:\Users");
        history.Navigate(@"C:\Users\Documents");
        history.Navigate(@"C:\Users\Pictures");

        // Assert
        Assert.Equal(@"C:\Users\Pictures", history.Current);
        Assert.Equal(3, history.History.Count);
    }

    [Fact]
    public void CanGoBack_AfterMultipleNavigations_ReturnsTrue()
    {
        // Arrange
        var history = new NavigationHistory();
        history.Navigate(@"C:\Users");
        history.Navigate(@"C:\Users\Documents");

        // Act & Assert
        Assert.True(history.CanGoBack);
    }

    [Fact]
    public void CanGoBack_AfterSingleNavigation_ReturnsFalse()
    {
        // Arrange
        var history = new NavigationHistory();
        history.Navigate(@"C:\Users");

        // Act & Assert
        Assert.False(history.CanGoBack);
    }

    [Fact]
    public void GoBack_ReturnsPreviousPath()
    {
        // Arrange
        var history = new NavigationHistory();
        history.Navigate(@"C:\Users");
        history.Navigate(@"C:\Users\Documents");

        // Act
        var result = history.GoBack();

        // Assert
        Assert.Equal(@"C:\Users", result);
        Assert.Equal(@"C:\Users", history.Current);
    }

    [Fact]
    public void GoForward_AfterGoBack_ReturnsNextPath()
    {
        // Arrange
        var history = new NavigationHistory();
        history.Navigate(@"C:\Users");
        history.Navigate(@"C:\Users\Documents");
        history.GoBack();

        // Act
        var result = history.GoForward();

        // Assert
        Assert.Equal(@"C:\Users\Documents", result);
        Assert.Equal(@"C:\Users\Documents", history.Current);
    }

    [Fact]
    public void Navigate_AfterGoBack_ClearsForwardHistory()
    {
        // Arrange
        var history = new NavigationHistory();
        history.Navigate(@"C:\Users");
        history.Navigate(@"C:\Users\Documents");
        history.Navigate(@"C:\Users\Pictures");
        history.GoBack();
        history.GoBack();

        // Act
        history.Navigate(@"C:\Users\Music");

        // Assert
        Assert.Equal(@"C:\Users\Music", history.Current);
        Assert.False(history.CanGoForward);
        Assert.Equal(2, history.History.Count);
    }

    [Fact]
    public void Navigate_DuplicateConsecutivePath_DoesNotAddToHistory()
    {
        // Arrange
        var history = new NavigationHistory();

        // Act
        history.Navigate(@"C:\Users");
        history.Navigate(@"C:\Users");

        // Assert
        Assert.Single(history.History);
    }

    [Fact]
    public void Navigated_Event_FiredOnNavigation()
    {
        // Arrange
        var history = new NavigationHistory();
        string? navigatedPath = null;
        history.Navigated += (_, path) => navigatedPath = path;

        // Act
        history.Navigate(@"C:\Users");

        // Assert
        Assert.Equal(@"C:\Users", navigatedPath);
    }
}
