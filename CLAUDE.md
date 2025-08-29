# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

AudioSpectrumPlayer is a WinUI 3 application built with .NET 9.0 that plays audio files and visualizes their spectrum in real-time. The application uses dependency injection, MVVM pattern with CommunityToolkit.Mvvm, and comprehensive logging with Serilog.

## Build Commands

```bash
# Debug build
dotnet build

# Release build with single-file publishing (as specified in Readme.md)
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true

# Build using VS Code task
dotnet build AudioSpectrumPlayer.csproj /property:GenerateFullPaths=true /consoleloggerparameters:NoSummary
```

## Development Workflows

### Implement

**When prompted to implement something, don't generate code right away, instead, follow these steps.**

- **Explore**:  
  - Find and read the relevant files  
  - Analyse how things work and how it interacts  
  - If you find inconsistencies or problems in the existing code, ask the user for clarification  

- **Plan**:  
  - Think about how to implement the requested feature without disrupting current functionality  
  - Analyse existing logic and patterns and how they could be extended without breaking exisiting code  
  - Try to keep things open for further extension, if you don't have enough information, ask the user for specifics instead of guessing  
  - If the user is not satisfied with the plan, repeat the planning step to find a better solution  

- **Code**:  
  - Once a satisfying plan has been established, implement the code.  
  - Try to adhere to existing patterns and architecture.  
  - Don't write excessive comments. Comments should be reserved for obscure patterns or workarounds  

- **Test**:  
  - For some parts of the codebase, test exist. If you find tests, ask to run the tests for the changed parts of the code  
  - If no tests exist for the changed code or code that interacts with it directly, ignore this step.  

### Review Code

**When prompted to review code, don't generate corrected code right away, instead, follow these steps**

- **Explore** :  Look at the code and how it connects to other parts of the codebase  
- **Search and explain**: When you find a potential problem, explain to the user what you found and why it is or may be a problem  
- **Correct**: If the user agrees that what you found is a problem, suggest a correction  

### Implement Tests

**When prompted to create tests, don't generate tests right away, instead, follow these steps**

- **Analyse**: Look at existing test that test similar features, look at how they are written and how extensive they are.
- **Plan**: Think about how the feature works, and what cases there may be.  
  - Tell the user what tests may be valuable, what should and what could be tested.  
  - Ask the user which tests should be implemented and which ones can be omited.  
- **Code**: Implement the tests which the user has approved in the previous step.  


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