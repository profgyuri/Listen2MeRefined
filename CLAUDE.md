# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build Listen2MeRefined.sln -c Debug

# Run tests (full suite)
dotnet test Listen2MeRefined.sln -c Debug

# Run tests (single project)
dotnet test Listen2MeRefined.Tests/Listen2MeRefined.Tests.csproj -c Debug

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Format code (run before PRs)
dotnet format

# Run the app
dotnet run --project Listen2MeRefined.WPF
```

To run a single test by name:
```bash
dotnet test Listen2MeRefined.Tests/ --filter "FullyQualifiedName~TestClassName.MethodName"
```

## Architecture

Clean Architecture with four layers, dependency flow: Core ← Application ← Infrastructure ← WPF.

- **Core**: Domain models, enums, repository interfaces. No dependencies on other layers.
- **Application**: Business logic interfaces and ViewModels. References Core only. Contains message types (pub/sub), navigation abstractions, and all ViewModel classes.
- **Infrastructure**: Concrete implementations — NAudio music player, EF Core + Dapper data access (SQLite), file scanning (TagLibSharp), waveform rendering (SkiaSharp), settings persistence.
- **WPF**: XAML views, DI module wiring, startup, theming (MaterialDesignThemes). Windows-only (`net10.0-windows7.0`).
- **Tests**: xUnit + Moq. Mirrors the production namespace structure.

### Modular DI

Services are grouped into `IModule` implementations in `Listen2MeRefined.WPF/Modules/`. Each module implements `RegisterServices()` and optionally `RegisterNavigation()` / `RegisterWindows()`. Modules are auto-discovered via assembly scanning (`appsettings.json: EnableAssemblyScan: true`). Navigation and windows are wired after the DI container is built.

### MVVM

ViewModels live in `Application/ViewModels/` and are bound by WPF views in `WPF/Views/`.

- Inherit from `ViewModelBase`
- Use `[ObservableProperty]` on private fields (CommunityToolkit.Mvvm source generator)
- Use `[RelayCommand]` on methods
- Subscribe to messages via `WeakReferenceMessenger` (MVVM Toolkit)
- Long-running init goes in `InitializeCoreAsync()` override
- Logging via Serilog is mandatory in ViewModels

### Music Playback Orchestration

`NAudioMusicPlayer` is a coordinator that delegates to focused collaborators:
- `ITrackLoader` — loads audio, returns `TrackLoadResult` (Success / MissingFile / UnsupportedFormat / CorruptFile)
- `IPlaybackOutput` — manages `WaveOutEvent` lifecycle and device reinitialization, returns `PlaybackOutputReconfigureResult`
- `IPlaybackProgressMonitor` — detects end-of-track via heuristics (position + state polling)
- `IPlaybackQueueService` — selects the next/previous track from the active queue

### Data Access

Two complementary approaches coexist:
- **EF Core** (`DataContext`) for migrations, structured queries, playlist/folder/settings entities
- **Dapper** (`IDbConnection` via `DbConnection`) for optimized read queries against the SQLite database

### Navigation

Shell windows (Main, CornerWindow, AdvancedSearch, FolderBrowser, Settings) each have a ViewModel in `Application/ViewModels/Shells/`. Navigation between views within a shell is handled by `INavigationService` / `INavigationRegistry`. `IWindowManager` / `IWindowRegistry` manage multi-window lifecycle.

## Conventions

- **Naming**: `PascalCase` for types/methods/properties, `_camelCase` for private fields, `I*` prefix for interfaces
- **File-scoped namespaces** throughout
- **Nullable reference types** enabled — avoid `!` suppression unless necessary
- **Interfaces required** for all public-facing classes registered in DI
- **Test naming**: `MethodName_State_ExpectedResult`
- **Commits**: imperative mood, scoped to one logical change

## Technology Stack

NAudio · CommunityToolkit.Mvvm · MaterialDesignThemes · SkiaSharp · TagLibSharp · Serilog · SharpHook · gong-wpf-dragdrop · EF Core · Dapper · SQLite
