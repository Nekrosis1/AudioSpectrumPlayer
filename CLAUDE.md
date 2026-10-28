# CLAUDE.md

## Project Overview

AudioSpectrumPlayer is a cross-platform (Linux + Windows) Avalonia desktop application built
with .NET 10 that plays audio files and visualizes their spectrum. Playback uses libvlc via
LibVLCSharp. The application uses dependency injection, the MVVM pattern with
CommunityToolkit.Mvvm, and logging with Serilog.

### Repository Layout

```
/                               (repo root — open this folder in VS Code)
├── AudioSpectrumPlayer.slnx    # the only solution
├── .vscode/                    # launch.json (normal run + "open file arg") and tasks.json (build)
├── .editorconfig
├── CLAUDE.md
├── scripts/agent-build.sh      # quiet build wrapper for agents (see Build and Publish)
├── libvlcsharp-3.9.7.1/        # gitignored, read-only reference source (see below)
└── AudioSpectrumPlayer/        # the app project
    ├── AudioSpectrumPlayer.csproj
    └── publish.sh
```

### Build and Publish

Agents build with `scripts/agent-build.sh` (see the `agent-build` skill), never with `dotnet`
directly. Running, testing and publishing stay with the user.

`AudioSpectrumPlayer/publish.sh` produces self-contained single-file builds into
`AudioSpectrumPlayer/Release/<rid>` (default: `win-x64` and `linux-x64`).

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

### Startup and Dependency Injection
- `Program.cs` builds the Avalonia app; `App.axaml.cs` sets up logging and a
  `Microsoft.Extensions.Hosting` host in `ConfigureServices()`. All services, view models and
  `MainWindowView` are registered as singletons.
- Services can be accessed via `App.GetService<T>()` (nullable) or `App.GetRequiredService<T>()`
  (throws), but prefer constructor injection.
- On shutdown the host is disposed, which disposes all services — important for
  `AudioPlayerService`, which holds native libvlc handles.
- "Open with" / command-line: the first argument is treated as an audio file path and loaded
  after startup (`App.LoadFileFromArgs`).

### Core Services
- **IAudioPlayerService / AudioPlayerService**: playback via LibVLCSharp (load, play, pause,
  stop, seek, volume, playback events). Also contains the native-library resolver that finds
  the system libvlc on Linux.
- **IAudioFileService / AudioFileService**: file picker and file validation.
- **IAudioStateService / AudioStateService**: current file, position, duration and playback
  state, exposed as events for the UI.
- **ITopLevelProvider / TopLevelProvider**: gives services access to the window's `TopLevel`
  (e.g. for the file picker) without depending on a concrete window.
- **SpectrumGenerationService**: FFT (MathNet.Numerics, Hamming window, log-frequency binning);
  raises `SpectrumDataUpdated` with the bar values.
- **SpectrumVisualizationService**: unfinished scaffolding — injected into
  `MainWindowViewModel` but not doing anything yet.

### Audio / Spectrum Pipeline — current state (the spectrum is NOT working yet)
1. Audio is loaded and played through libvlc (`AudioPlayerService`). This works.
2. **PCM input is a stub**: `SpectrumGenerationService.GetAudioChunkAtPosition` returns silence.
   The old NAudio-based source was removed in the libvlc migration (`Services/FileToPCM.cs` is
   fully commented out). Feeding real PCM, e.g. via libvlc's audio callbacks, is still to do.
3. **Nothing triggers updates**: no code calls `SpectrumGenerationService.UpdateSpectrumData()`.
4. The FFT and the drawing side are in place: `SpectrumControlView` subscribes directly to
   `SpectrumDataUpdated` and renders the bars as a polygon.

### MVVM Implementation
- ViewModels derive from `ViewModelBase` and use `[ObservableProperty]` from CommunityToolkit.Mvvm.
- Compiled bindings are on by default (`AvaloniaUseCompiledBindingsByDefault`).
- `MainWindowViewModel` coordinates playback and UI state; `LogViewModel` backs the in-app log.
- `ViewLocator` maps `XxxViewModel` → `XxxView` by name, so views keep the `-View` suffix.

### Logging
- Serilog writes to Debug output, to a rolling file, and to the in-app log viewer
  (custom `LogDisplaySink` in `Logging/`).
- Log files go to `logs/log-[Date].txt` in the application (build output) directory.

### Key Components (`Views/`)
- **MainWindowView**: primary window, hosts the other views
- **MenuBarControlView**: application menu
- **PlaybackProgressControlView**: seek bar and position display
- **VolumeControlView**: volume
- **SpectrumControlView**: spectrum display
- **LogDisplayView**: in-app log viewer
- **ErrorDisplayView**: error overlay (e.g. bad/missing file)

### File Structure (inside `AudioSpectrumPlayer/`)
- `Services/`: core logic, playback and audio processing
- `Interfaces/`: service contracts
- `ViewModels/`: MVVM view models
- `Views/`: Avalonia `.axaml` views, code-behind and converters
- `Logging/`: custom Serilog sink
- `Assets/`: icons and other Avalonia resources

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

## LibVLCSharp Reference Source

`libvlcsharp-3.9.7.1/` in the repo root is the **source code of LibVLCSharp 3.9.7.1**, the same
version the app references via NuGet. It is there only so agents can read it — it is gitignored,
not part of the solution, and not built. When unsure how a LibVLCSharp API behaves (e.g.
`MediaPlayer`, `Media`, audio callbacks), read it there instead of guessing; the library code
is under `src/`, examples under `samples/`. If the NuGet version is upgraded, this copy should
be replaced to match.

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


