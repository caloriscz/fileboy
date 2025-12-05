# FileBoy Architecture

## Overview

FileBoy is a media/file manager with a WPF + Blazor Hybrid architecture.

```
┌─────────────────────────────────────────────────────────────┐
│                    FileBoy.App (WPF)                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │                   MainWindow.xaml                     │  │
│  │  ┌─────────────────────────────────────────────────┐  │  │
│  │  │              BlazorWebView                      │  │  │
│  │  │  ┌───────────────────────────────────────────┐  │  │  │
│  │  │  │         FileBoy.UI (Blazor)               │  │  │  │
│  │  │  │  - Pages (Browser, Detail, Settings)      │  │  │  │
│  │  │  │  - Components (Thumbnail, FileList, etc)  │  │  │  │
│  │  │  └───────────────────────────────────────────┘  │  │  │
│  │  └─────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
           │                              │
           ▼                              ▼
┌─────────────────────┐      ┌─────────────────────────────┐
│   FileBoy.Core      │      │   FileBoy.Infrastructure    │
│   - Models          │◄─────│   - FileSystemService       │
│   - Interfaces      │      │   - ThumbnailGenerator      │
│   - Business Logic  │      │   - ProcessLauncher         │
└─────────────────────┘      └─────────────────────────────┘
```

## Project Responsibilities

### FileBoy.App
- WPF application entry point
- Hosts BlazorWebView in MainWindow
- DI container configuration
- Serilog initialization
- Native Windows integrations (if needed)

### FileBoy.Core
- Domain models (`FileItem`, `FolderItem`, `GalleryMetadata`)
- Service interfaces (`IFileSystemService`, `IThumbnailService`)
- Business logic (pure C#, no external dependencies)
- Extensions and helpers

### FileBoy.UI
- Razor Class Library
- Blazor pages and components
- CSS/styling in wwwroot
- UI state management

### FileBoy.Infrastructure  
- Interface implementations
- File system operations
- Process launching (open with associated app)
- Future: Database, AI integrations

## Key Flows

### File Navigation
```
User enters path → NavigationService.NavigateAsync(path)
    → FileSystemService.GetFilesAsync(path)
    → Returns IEnumerable<FileItem>
    → UI updates via state change
```

### Open File
```
User double-clicks file → Check if image
    → If image: Navigate to DetailPage
    → If other: ProcessLauncher.OpenWithDefault(filePath)
```

### Thumbnail Generation
```
BrowserPage loads → ThumbnailService.GetThumbnailsAsync(files)
    → Generates/caches thumbnails
    → Returns as base64 or file paths
```

## Future Extensions
- **Tagging**: Add `TagService` in Core, storage in Infrastructure
- **Gallery Descriptions**: Extend `FolderItem` with metadata
- **AI Integration**: New service in Infrastructure, interface in Core
