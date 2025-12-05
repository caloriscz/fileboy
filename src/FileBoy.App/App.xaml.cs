using System.IO;
using System.Windows;
using FileBoy.Core.Interfaces;
using FileBoy.Infrastructure.FileSystem;
using FileBoy.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace FileBoy.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure Serilog
        var logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FileBoy", "logs", "fileboy-.log");

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day)
            .WriteTo.Console()
            .CreateLogger();

        // Configure services
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        Log.Information("FileBoy starting up");

        // Create and show main window
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Blazor WebView services (required)
        services.AddWpfBlazorWebView();

        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // Memory cache for thumbnails
        services.AddMemoryCache(options => options.SizeLimit = 500);

        // Core services
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<INavigationHistory, NavigationHistory>();
        services.AddSingleton<IProcessLauncher, ProcessLauncher>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IFFmpegManager, FFmpegManager>();
        services.AddSingleton<IVideoThumbnailService, VideoThumbnailService>();
        services.AddSingleton<IThumbnailService, ThumbnailService>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("FileBoy shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

