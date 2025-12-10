using System.IO;
using FileBoy.App.ViewModels;
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

    protected override void OnStartup(System.Windows.StartupEventArgs e)
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
        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
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
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IFolderHistoryService, FolderHistoryService>();
        services.AddSingleton<IImageEditorService, ImageEditorService>();

        // Page navigation service
        services.AddSingleton<Services.PageNavigationService>();
        services.AddSingleton<Services.IPageNavigationService>(sp => sp.GetRequiredService<Services.PageNavigationService>());

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DetailViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<CopyMoveToFolderViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        Log.Information("FileBoy shutting down");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}

