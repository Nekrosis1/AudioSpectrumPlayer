# CLAUDE.md

## Project Overview

AudioSpectrumPlayer is a WinUI 3 application built with .NET 9.0 that plays audio files and visualizes their spectrum in real-time. The application uses dependency injection, MVVM pattern with CommunityToolkit.Mvvm, and comprehensive logging with Serilog.
AudioSpectrumPlayer.Avalonia is an Avalonia application built with .NET 10, same features but now also works on Linux.

### Build and Publish

The user will always build and publish himself, no need to run `bash dotnet ...`

#### Distribution differs by platform (Windows vs Linux)

The shared goal is that an end user never sees "cannot start, X not found" — they download
the app and it just runs (or installs and runs). HOW that goal is met differs per platform,
because Windows and Linux have opposite conventions for dependencies:

- **Windows:** bundle everything into a self-contained build (the .NET runtime, native libs,
  assets). Native deps come from bundling NuGet packages (e.g. `VideoLAN.LibVLC.Windows`).
  Windows has no system package manager handling these, so the app must carry them.
- **Linux:** do NOT bundle. Use distro packaging (e.g. a `PKGBUILD` for Arch) and let the
  package manager dynamically link system libraries (e.g. system `libvlc`). The package
  metadata declares the dependency, so the user still gets a working install without manual
  setup — that's the idiomatic Linux way. Bundling everything goes against the philosophy of
  most Linux packaging (Snap/Flatpak being the exceptions).

This is also why some native libs ship NuGet packages only for Windows/Mac and **not Linux**
(e.g. `VideoLAN.LibVLC.Linux` does not exist): on Linux you're expected to depend on the
system-installed library, not bundle your own copy.

## Working Style

I work in a **pair-programming style** — I want to stay in the loop and keep control, not
hand off a task and get a finished result. Prefer working **step by step**: take a small,
focused action, then report back, rather than completing many things in one autonomous run.

- **Avoid large batches of operations.** Many small steps with check-ins are preferable to
  one big sweep, even if that means more interruptions. I'd rather be asked than surprised.
- When a step reveals a decision or an ambiguity, **pause and talk to me** instead of
  picking and pressing on.
- This is a deliberate preference for *this* project; optimize for collaboration, not for
  finishing with the fewest round-trips.

## Environment

- **New to VS Code**: The user has experience in Visual Studio on Windows, but is now working on Arch Linux, with VS Code. If he has questions why things don't work, think about how it may be different in VS or on Windows, as he may not know some seemingly obvious things.

### Package Management
  ```bash
  # Check for outdated NuGet packages (requires dotnet-outdated tool)
  dotnet outdated

  # Upgrade all packages within their current major version
  dotnet outdated -u -vl Major
  ```

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

## Avalonia Reference Material

When unsure how to use an Avalonia API, type, control, or theme resource, **consult the official
documentation first** — don't guess at type names or rely on memory of WinUI/WPF equivalents:

- Main docs site: https://docs.avaloniaui.net/
- API reference (search-friendly): https://docs.avaloniaui.net/api/
- Fluent theme entry point: https://docs.avaloniaui.net/api/avalonia/themes/fluent
  (The top page is mostly empty, but it links to every type/resource the Fluent theme exposes.)

Reasoning:
- Avalonia's NuGet packages ship XAML compiled into DLLs — you cannot just `find` the source
  `.axaml` files locally. The `.xml` files in the package are only IntelliSense comments.
- The Avalonia GitHub source is also a valid reference but is large and noisy compared to the docs.

**If you can't find what you need in one or two doc-page fetches, ask the user.** He can help
locate the correct page faster than blind navigation through many web requests.

## UI layout philosophy: content-driven boxes

This is about *how* UI elements are arranged, not how they look.

**Core rule: a component's layout box (its slot/cell/bounds) must be bound to the component.**
When I change a component's size, its box
should grow or shrink to match, and *neighbors get pushed*, instead of the component
overflowing and encroaching on another component.
When I set the size of a box, it content should be made to fit, getting smaller or larger,
instead of being stuck at the beginning or getting cut off.

The test I use: "If I want this element 20% bigger, can I change *one* value and have
its box follow and the neighbors move — without touching 13 other values and without
overlap?" If yes, the layout is built correctly.

Concrete guidance:
- **Prefer content-sizing for slots** (`Auto` columns/rows, intrinsic/shrink-to-fit
  sizing) over space-filling slots (`*`/star, `flex-grow`, fixed parent widths) for
  elements that should be exactly as big as their content. Star/fill slots ignore the
  child's desired size and hand it leftover space — when that space is too small the
  child overflows and overlaps its neighbors. That failure mode is the thing to avoid.
- **Don't stack two fixed sizes that fight.** A fixed outer width (a parent cap) plus a
  fixed inner width is the classic trap: if outer < inner + siblings + margins, the
  inner element overflows. Let the container size to its contents instead of capping it.
- **Minimize hardcoded sizing values**, but don't pretend zero is achievable: some
  elements have no intrinsic content size (e.g. a drawing canvas/polygon) and legitimately
  need one explicit size or ratio. That's fine — it's the *single source* for that element.
- **Margin lines up with the visible element only when the element fills its box.** If a
  drawn element is smaller than its slot, margins measure from the slot edge, not the
  visible edge, and feel unintuitive. Keep the visible content filling its box.


