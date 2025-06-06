using AudioSpectrumPlayer.Services;
using AudioSpectrumPlayer.ViewModels;
using Serilog;
using Uno.Resizetizer;

namespace AudioSpectrumPlayer;
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    protected IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            //.UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                // Enable localization (see appsettings.json for supported languages)
                //.UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    // Services
                    services.AddSingleton<IAudioFileService, AudioFileService>();

                    // ViewModels
                    services.AddSingleton<AudioPlayerViewModel>();
                    services.AddSingleton<LogViewModel>();
                    // Views
                    services.AddSingleton<MainWindow>();
                })
            //.UseNavigation(RegisterRoutes)
            );
        Host = builder.Build();

        // Create and show the main window
        MainWindow = Host.Services.GetRequiredService<MainWindow>();
        MainWindow.Activate();

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        //Host = await builder.NavigateAsync<Shell>();
        await ProcessCommandLineArgs();
    }

    //private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    //{
    //    views.Register(
    //        new ViewMap(ViewModel: typeof(ShellViewModel)),
    //        new ViewMap<MainPage>(),
    //        new DataViewMap<SecondPage, SecondViewModel, Entity>()
    //    );

    //    routes.Register(
    //        new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
    //            Nested:
    //            [
    //                new ("Main", View: views.FindByView<MainPage>(), IsDefault:true),
    //                new ("Second", View: views.FindByViewModel<SecondViewModel>()),
    //            ]
    //        )
    //    );
    //}

    private async Task ProcessCommandLineArgs()
    {
        try
        {
            Log.Debug("Processing command line arguments");
            string[] launchArgs = Environment.GetCommandLineArgs();

            for (int i = 0; i < launchArgs.Length; i++)
            {
                Log.Debug($"Arg[{i}]: {launchArgs[i]}");
            }

            if (launchArgs.Length > 1)
            {
                string filePath = launchArgs[1];
                Log.Debug($"Found file argument to open: {filePath}");

                try
                {
                    // Check if file exists
                    if (System.IO.File.Exists(filePath))
                    {
                        // Get the AudioPlayerViewModel from DI
                        var audioPlayerViewModel = Host?.Services.GetRequiredService<AudioPlayerViewModel>();
                        var audioFileService = Host?.Services.GetRequiredService<IAudioFileService>();

                        if (audioPlayerViewModel != null && audioFileService != null)
                        {
                            if (audioFileService.IsValidAudioFile(filePath))
                            {
                                await audioPlayerViewModel.LoadAudioFileAsync(filePath);
                                Log.Information($"Successfully loaded file from command line: {filePath}");
                            }
                            else
                            {
                                Log.Warning($"Invalid audio file format: {filePath}");
                            }
                        }
                        else
                        {
                            Log.Error("AudioPlayerViewModel or AudioFileService not available from DI");
                        }
                    }
                    else
                    {
                        Log.Error($"File does not exist: {filePath}");
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    Log.Error($"Access denied to file: {filePath}");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, $"Error processing file: {filePath}");
                }
            }
            else
            {
                Log.Debug("No command line arguments provided");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing command line arguments");
        }
    }

    public static T? GetService<T>() where T : class
    {
        return ((App)Current).Host?.Services.GetService<T>();
    }

    public static T GetRequiredService<T>() where T : class
    {
        return ((App)Current).Host?.Services.GetRequiredService<T>()
            ?? throw new InvalidOperationException($"Service {typeof(T).Name} not found");
    }

}
