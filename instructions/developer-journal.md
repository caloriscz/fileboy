# Developer Journal - FileBoy

## Template

Use this format for daily entries:

```markdown
## YYYY-MM-DD

### Planned
- [ ] Task 1
- [ ] Task 2

### Completed
- [x] What was done
- [x] What was done

### Technical Decisions
- **Decision**: Why this approach was chosen

### Issues & Solutions
- **Problem**: Description
- **Solution**: How it was resolved

### Next Steps
- Upcoming work
- Risks to monitor
```

---

## 2025-12-05

### Planned
- [x] Initialize project structure
- [x] Set up AI coding instructions

### Completed
- [x] Created `.github/copilot-instructions.md` with project conventions
- [x] Established architecture documentation
- [x] Created 6-project solution structure (4 src + 2 tests)
- [x] Implemented FileBoy.Core with models, interfaces, enums, extensions
- [x] Implemented FileBoy.Infrastructure with all service implementations
- [x] Created FileBoy.UI with Blazor components (PathBar, FileList, ThumbnailGrid, BrowserPage)
- [x] Configured FileBoy.App with WPF + BlazorWebView host
- [x] Set up DI container with Serilog logging
- [x] Created 46 unit tests (all passing)

### Technical Decisions
- **WPF + Blazor Hybrid**: Chosen for modern UI capabilities while maintaining Windows desktop integration. BlazorWebView allows using Razor components inside WPF.
- **4-Project Structure**: Separates concerns - App (host), Core (logic), UI (components), Infrastructure (external)
- **Serilog**: Industry standard for structured logging, rolling file support
- **Memory-only thumbnail cache**: Using IMemoryCache with LRU eviction (500 items limit)
- **InvariantCulture for formatting**: File sizes use invariant culture for consistent display

### Issues & Solutions
- **Package version conflict**: Microsoft.Extensions packages needed to be v10.0.0 to match .NET 10
- **Culture-dependent tests**: FormatFileSize used locale-specific decimal separator; fixed with InvariantCulture

### Next Steps
- Implement Detail view page for image preview
- Add classic menu functionality (File, View, Options, Help)
- Implement FFmpeg auto-download for video thumbnails
- Add proper thumbnail scaling (currently returns full images)

### Risks
- FFmpeg auto-download needs reliable source and error handling
