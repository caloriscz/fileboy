# FileBoy - AI Coding Instructions

## Project Overview
FileBoy is a media/file manager application (similar to XnView/classic ACDSee) built with WPF + Blazor Hybrid. The app provides list/thumbnail views, file navigation, and image preview capabilities.

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
- **WPF + Blazor Hybrid** - WPF hosts BlazorWebView for modern UI
- **Serilog** - Structured logging
- **Microsoft.Extensions.DependencyInjection** - DI container
- **xUnit** - Unit testing framework

## Architecture

### Project Structure
```
src/
├── FileBoy.App/           # WPF host (MainWindow hosts BlazorWebView)
├── FileBoy.Core/          # Business logic, models, interfaces (no UI deps)
├── FileBoy.UI/            # Blazor components (Razor Class Library)
└── FileBoy.Infrastructure/ # File system operations, external integrations
tests/
├── FileBoy.Core.Tests/
└── FileBoy.Infrastructure.Tests/
```

### SOLID Principles (Strictly Enforced)
- **Services**: One responsibility per class (e.g., `FileNavigationService`, `ThumbnailService`)
- **Interfaces**: Define in `FileBoy.Core/Interfaces/`, implement in Infrastructure
- **DI**: All services registered in `App.xaml.cs` via `IServiceCollection`

### Key Patterns
```csharp
// Service registration pattern (App.xaml.cs)
services.AddSingleton<IFileSystemService, FileSystemService>();
services.AddSingleton<IThumbnailService, ThumbnailService>();
services.AddSerilog(config => config.WriteTo.File("logs/fileboy-.log", rollingInterval: RollingInterval.Day));

// Interface-first design
public interface IFileSystemService
{
    Task<IEnumerable<FileItem>> GetFilesAsync(string path);
    Task<FileItem> GetFileDetailsAsync(string filePath);
}
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

## Blazor Hybrid Conventions

### Component Location
- **Pages**: `FileBoy.UI/Pages/` - Full page components (e.g., `BrowserPage.razor`, `DetailPage.razor`)
- **Components**: `FileBoy.UI/Components/` - Reusable UI (e.g., `ThumbnailGrid.razor`, `FileList.razor`)
- **Shared**: `FileBoy.UI/Shared/` - Layouts, common components

### State Management
Use cascading parameters or inject services for shared state:
```csharp
@inject INavigationState NavigationState
@inject IFileSystemService FileService
```

### Navigation History Service
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
