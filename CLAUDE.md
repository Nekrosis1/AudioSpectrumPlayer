# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AudioSpectrumPlayer is a WinUI 3 application built with .NET 9.0 that plays audio files and visualizes their spectrum in real-time. The application uses dependency injection, MVVM pattern with CommunityToolkit.Mvvm, and comprehensive logging with Serilog.
AudioSpectrumPlayer.Avalonia is an Avalonia application built with .NET 10, same features but now also works on Linux.

### Build and Publish

The user will always build and publish himself, no need to run `bash dotnet ...`

## Environment

- **New to VS Code**: The user has experience in Visual Studio on Windows, but is now working on Arch Linux, with VS Code. If he has questions why things don't work, think about how it may be different in VS or on Windows, as he may not know some seemingly obvious things.

### Package Management
  ```bash
  # Check for outdated NuGet packages (requires dotnet-outdated tool)
  dotnet outdated

  # Upgrade all packages within their current major version
  dotnet outdated -u -vl Major

## Architecture

### Dependency Injection
Services are configured in `App.xaml.cs:ConfigureServices()` and can be accessed via:
- `App.GetService<T>()` - nullable service retrieval
- `App.GetRequiredService<T>()` - throws if service not found

### Core Services
- **IAudioFileService**: File selection and validation
- **IAudioStateService**: Playback state management and PCM data handling
- **SpectrumVisualizationService**: Real-time spectrum visualization
- **SpectrumGenerationService**: FFT processing for spectrum analysis

### MVVM Implementation
- ViewModels use `[ObservableProperty]` from CommunityToolkit.Mvvm
- MainWindowViewModel coordinates audio playback and UI state
- LogViewModel manages application logging display

### Audio Processing Pipeline
1. Audio files loaded via Windows MediaPlayer API
2. PCM data extracted using FileToPCM service
3. FFT processing in SpectrumGenerationService
4. Real-time visualization in SpectrumVisualizationService

### Logging
- Serilog configured for Debug output, file logging, and custom LogDisplay sink
- Custom LogDisplaySink writes to in-app log viewer
- Log files written to `logs/log-[Date].txt` in application directory

### Key Components
- **MainWindow**: Primary application window with spectrum visualization
- **MenuBarControl**: Application menu and controls
- **PlaybackProgressControl**: Seek bar and position display
- **VolumeControl**: Audio volume management
- **SpectrumControl**: Audio spectrum visualization display
- **LogDisplay**: In-application log viewer

### File Structure
- `Services/`: Core business logic and audio processing
- `ViewModels/`: MVVM view models
- `Views/`: WinUI 3 XAML views and code-behind
- `Interfaces/`: Service contracts

### Code Style Guidelines
- use `using` statements at the top of the file, rather than adding namespaces to classes when using them.
  - Example: Use `builder.Services.AddScoped<IMyService, MyService>();` instead of `builder.Services.AddScoped<MyNamespace.IMyService, MyNamespace.MyService>();`
