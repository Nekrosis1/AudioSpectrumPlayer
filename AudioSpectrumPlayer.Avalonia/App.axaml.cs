using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using AudioSpectrumPlayer.Avalonia.Interfaces;
using AudioSpectrumPlayer.Avalonia.Services;
using AudioSpectrumPlayer.Avalonia.ViewModels;
using AudioSpectrumPlayer.Avalonia.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;

namespace AudioSpectrumPlayer.Avalonia;

public partial class App : Application
{
    private IHost? _host;

    public App()
    {
        ConfigureLogging();
        ConfigureServices();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Get MainWindow from DI container
            desktop.MainWindow = _host?.Services.GetRequiredService<MainWindow>();

            SetupExceptionHandling();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ConfigureLogging()
    {
        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithThreadId()
            .WriteTo.Debug(
                restrictedToMinimumLevel: LogEventLevel.Information,
                outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}");

        Log.Logger = loggerConfig.CreateLogger();
        Log.Information("AudioSpectrumPlayer Avalonia starting");
        Log.Information($"Logs written to {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-[Date].txt")}");
    }

    private void ConfigureServices()
    {
        var hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                // Services
                services.AddSingleton<IAudioFileService, AudioFileService>();
                services.AddSingleton<IAudioStateService, AudioStateService>();
                services.AddSingleton<SpectrumVisualizationService>();
                services.AddSingleton<SpectrumGenerationService>();

                // ViewModels
                services.AddSingleton<MainWindowViewModel>();

                // Views
                services.AddSingleton<MainWindow>();
            });

        _host = hostBuilder.Build();
        Log.Debug("DI container configured with {ServiceCount} services", _host.Services.GetType().Name);
    }

    private static void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            Log.Fatal("Unhandled AppDomain exception");
            if (args.ExceptionObject is Exception ex)
            {
                Log.Error(ex, "AppDomain.UnhandledException");
            }
            else
            {
                Log.Error($"Unknown exception type: {args.ExceptionObject?.GetType().ToString() ?? "null"}");
            }
        };
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    // Helper methods to get services from anywhere in the app
    public static T? GetService<T>() where T : class
    {
        if (Current is App app)
        {
            return app._host?.Services.GetService<T>();
        }
        return null;
    }

    public static T GetRequiredService<T>() where T : class
    {
        if (Current is App app)
        {
            return app._host?.Services.GetRequiredService<T>()
                ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found");
        }
        throw new InvalidOperationException("Application instance not available");
    }
}