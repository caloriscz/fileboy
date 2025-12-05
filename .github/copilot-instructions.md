# FileBoy - AI Coding Instructions

## Project Overview
FileBoy is a media/file manager application (similar to XnView/classic ACDSee) built with **WPF** using **MVVM** pattern. The app provides list/thumbnail views, file navigation, and image preview capabilities.

### Core Behaviors
- **Default view**: List view (user can switch to thumbnail)
- **Image preview**: Double-click opens built-in detail view for images
- **Other files**: Double-click opens with Windows associated program
- **Navigation**: Path bar with back/forward history (browser-style)

### Supported Image Formats
Built-in viewer supports (static display only):
- **Standard**: `.jpg`, `.jpeg`, `.png`, `.gif` (static), `.bmp`, `.webp`
- **Windows**: `.ico`, `.tiff`
- **No RAW support** - opens in external program

### Video Thumbnails
- Extract first frame for thumbnail display
- Use **FFmpeg** (via `FFMpegCore` NuGet package)
- **FFmpeg binaries**: Download on first use to `%LOCALAPPDATA%/FileBoy/ffmpeg/`
- Videos open in external player (not built-in)
```csharp
// Video thumbnail extraction pattern
public interface IVideoThumbnailService
{
    Task<byte[]?> GetThumbnailAsync(string videoPath, CancellationToken ct = default);
}

// FFmpeg binary management
public interface IFFmpegManager
{
    Task<bool> EnsureAvailableAsync(CancellationToken ct = default);
    string FFmpegPath { get; }
}
```

## Technology Stack
- **.NET 10** - Target framework
- **WPF** - Windows Presentation Foundation for UI
- **MVVM** - Model-View-ViewModel pattern (CommunityToolkit.Mvvm)
- **Serilog** - Structured logging
- **Microsoft.Extensions.DependencyInjection** - DI container
- **xUnit** - Unit testing framework

## Architecture

### Project Structure
```
src/
├── FileBoy.App/           # WPF application (Views, ViewModels, App startup)
│   ├── Views/             # XAML views (MainWindow, DetailWindow)
│   ├── ViewModels/        # ViewModels with INotifyPropertyChanged
│   ├── Converters/        # IValueConverters for XAML bindings
│   └── Resources/         # Styles, templates, icons
├── FileBoy.Core/          # Business logic, models, interfaces (no UI deps)
└── FileBoy.Infrastructure/ # File system operations, external integrations
tests/
├── FileBoy.Core.Tests/
└── FileBoy.Infrastructure.Tests/
```

### MVVM Pattern (Strictly Enforced)
- **ViewModels**: Inherit from `ObservableObject` (CommunityToolkit.Mvvm)
- **Commands**: Use `RelayCommand` and `AsyncRelayCommand` from CommunityToolkit.Mvvm
- **Services**: One responsibility per class (e.g., `FileSystemService`, `ThumbnailService`)
- **Interfaces**: Define in `FileBoy.Core/Interfaces/`, implement in Infrastructure
- **DI**: All services registered in `App.xaml.cs` via `IServiceCollection`

### Key Patterns
```csharp
// ViewModel pattern with CommunityToolkit.Mvvm
public partial class MainViewModel : ObservableObject
{
    private readonly IFileSystemService _fileSystemService;
    
    [ObservableProperty]
    private string _currentPath = string.Empty;
    
    [ObservableProperty]
    private ObservableCollection<FileItemViewModel> _items = [];
    
    [RelayCommand]
    private async Task NavigateToPath(string path)
    {
        // Navigation logic
    }
}

// Service registration pattern (App.xaml.cs)
services.AddSingleton<IFileSystemService, FileSystemService>();
services.AddSingleton<IThumbnailService, ThumbnailService>();
services.AddSingleton<MainViewModel>();
services.AddSerilog(config => config.WriteTo.File("logs/fileboy-.log", rollingInterval: RollingInterval.Day));
```

### WPF UI Components
- **DataGrid**: For list view with columns (Name, Size, Type, Date Modified)
- **ListBox with WrapPanel**: For thumbnail grid view
- **Menu**: Classic File, View, Options, Help menu
- **ToolBar**: Navigation buttons, view toggle
- **TextBox**: Path input with Enter key navigation

```xaml
<!-- DataGrid for list view -->
<DataGrid ItemsSource="{Binding Items}" 
          SelectedItem="{Binding SelectedItem}"
          AutoGenerateColumns="False"
          IsReadOnly="True"
          SelectionMode="Extended">
    <DataGrid.InputBindings>
        <MouseBinding MouseAction="LeftDoubleClick" 
                      Command="{Binding OpenItemCommand}"/>
    </DataGrid.InputBindings>
    <DataGrid.Columns>
        <DataGridTemplateColumn Header="Name" Width="*">
            <DataGridTemplateColumn.CellTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal">
                        <Image Source="{Binding Icon}" Width="16" Height="16"/>
                        <TextBlock Text="{Binding Name}" Margin="4,0,0,0"/>
                    </StackPanel>
                </DataTemplate>
            </DataGridTemplateColumn.CellTemplate>
        </DataGridTemplateColumn>
        <DataGridTextColumn Header="Size" Binding="{Binding FormattedSize}" Width="80"/>
        <DataGridTextColumn Header="Type" Binding="{Binding TypeDescription}" Width="150"/>
        <DataGridTextColumn Header="Date Modified" Binding="{Binding ModifiedDate, StringFormat='{}{0:dd.MM.yyyy HH:mm}'}" Width="130"/>
    </DataGrid.Columns>
</DataGrid>
```

## Application Settings
- **Storage**: JSON file in `%APPDATA%/FileBoy/settings.json`
- **Pattern**: Use `IOptions<T>` pattern with `AppSettings` class
```csharp
public class AppSettings
{
    public ViewMode DefaultView { get; set; } = ViewMode.List;
    public string LastPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
    public List<string> PathHistory { get; set; } = [];
}
```

## Thumbnail Caching
- **Strategy**: Memory-only cache (no disk persistence)
- **Implementation**: Use `IMemoryCache` from `Microsoft.Extensions.Caching.Memory`
- **Eviction**: LRU with configurable size limit
```csharp
services.AddMemoryCache(options => options.SizeLimit = 500); // ~500 thumbnails
```

## Navigation History Service
```csharp
public interface INavigationHistory
{
    string Current { get; }
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    void Navigate(string path);
    string GoBack();
    string GoForward();
}
```

## Commands

### Build & Run
```powershell
dotnet build src/FileBoy.App/FileBoy.App.csproj
dotnet run --project src/FileBoy.App/FileBoy.App.csproj
```

### Testing
```powershell
dotnet test tests/FileBoy.Core.Tests/
dotnet test --collect:"XPlat Code Coverage"  # With coverage
```

## Logging with Serilog
```csharp
// Inject ILogger<T> - Serilog integrates with Microsoft.Extensions.Logging
public class FileSystemService(ILogger<FileSystemService> logger)
{
    public async Task<IEnumerable<FileItem>> GetFilesAsync(string path)
    {
        logger.LogInformation("Loading files from {Path}", path);
        // ...
    }
}
```

## Code Generation Guidelines
- Always inject dependencies via constructor
- Create interface in Core, implementation in Infrastructure
- Include `CancellationToken` in async methods
- Use `ValueTask` for hot paths, `Task` otherwise
- Generate xUnit tests with AAA pattern for new services
- Use `[Theory]` with `[InlineData]` for parameterized tests
- Use CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`)
