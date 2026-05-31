using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AudioSpectrumPlayer.Avalonia.Interfaces;
using AudioSpectrumPlayer.Avalonia.Logging;
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
			var mainWindow = _host?.Services.GetRequiredService<MainWindow>();
			desktop.MainWindow = mainWindow;

			// Now that the window exists, hand it to the provider so services
			// (e.g. the file picker) can reach the TopLevel without depending
			// on a concrete Window.
			if (mainWindow is not null)
			{
				_host?.Services.GetRequiredService<TopLevelProvider>().TopLevel = mainWindow;
			}

			SetupExceptionHandling();

			// Tear down the DI host when the app is shutting down. The host owns every
			// service as a singleton, so disposing it cascades Dispose() to all of them —
			// most importantly AudioPlayerService, which holds native libvlc handles
			// (MediaPlayer/LibVLC/Media) that are otherwise only freed by process teardown.
			// ShutdownRequested fires once, before the app actually exits.
			desktop.ShutdownRequested += OnShutdownRequested;

			// "Open with" / command-line launch: the OS passes the chosen file as the
			// first argument. Load it once the UI loop is running so the window is shown
			// and the error overlay (on a bad/missing path) has somewhere to render.
			LoadFileFromArgs(desktop.Args);
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
				outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
			.WriteTo.LogDisplay(
				restrictedToMinimumLevel: LogEventLevel.Information);

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
				services.AddSingleton<TopLevelProvider>();
				services.AddSingleton<ITopLevelProvider>(sp => sp.GetRequiredService<TopLevelProvider>());
				services.AddSingleton<IAudioPlayerService, AudioPlayerService>();
				services.AddSingleton<IAudioFileService, AudioFileService>();
				services.AddSingleton<IAudioStateService, AudioStateService>();
				services.AddSingleton<SpectrumVisualizationService>();
				services.AddSingleton<SpectrumGenerationService>();

				// ViewModels
				services.AddSingleton<MainWindowViewModel>();
				services.AddSingleton<LogViewModel>();

				// Views
				services.AddSingleton<MainWindow>();
			});

		_host = hostBuilder.Build();
		Log.Debug("DI container configured with {ServiceCount} services", _host.Services.GetType().Name);
	}

	// Loads the file named by the first command-line argument, if any. The error
	// handling (missing file, unsupported format) lives in LoadAudioFileAsync, which
	// surfaces failures through the in-app error overlay.
	private void LoadFileFromArgs(string[]? args)
	{
		var filePath = args?.FirstOrDefault();
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return;
		}

		var viewModel = _host?.Services.GetService<MainWindowViewModel>();
		if (viewModel is null)
		{
			return;
		}

		Log.Information("Loading file from command-line argument: {FilePath}", filePath);
		Dispatcher.UIThread.Post(async () => await viewModel.LoadAudioFileAsync(filePath));
	}

	private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
	{
		Log.Information("Shutdown requested — disposing services");
		_host?.Dispose();
		_host = null;
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