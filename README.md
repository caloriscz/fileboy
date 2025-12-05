# FileBoy

A modern media and file manager for Windows, inspired by XnView and classic ACDSee.

## Features

- 📁 List and thumbnail view modes
- 🖼️ Built-in image viewer
- 🔍 Quick path navigation
- 🚀 Open files with associated programs

### Planned Features
- 🏷️ File tagging system
- 📝 Gallery descriptions
- 🤖 AI-powered organization

## Technology

- .NET 10
- WPF + Blazor Hybrid
- Serilog logging

## Getting Started

```powershell
# Build
dotnet build

# Run
dotnet run --project src/FileBoy.App/FileBoy.App.csproj

# Test
dotnet test
```

## Project Structure

```
src/
├── FileBoy.App/           # WPF application host
├── FileBoy.Core/          # Business logic & models
├── FileBoy.UI/            # Blazor UI components
└── FileBoy.Infrastructure/ # File system & external services
```

## License

[Your license here]
