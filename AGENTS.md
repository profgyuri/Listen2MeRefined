# Repository Guidelines

## Project Structure & Module Organization
This repository is a C#/.NET solution centered on `Listen2MeRefined`. Keep code organized by project responsibility (for example: UI, infrastructure, and shared/domain logic).  
Typical layout:
- `*.sln`: solution entry point
- `Listen2MeRefined.WPF/`: desktop UI (views, view models, UI utilities)
- `Listen2MeRefined.Infrastructure/`: persistence, integrations, system hooks
- `*/Data/`, `*/Services/`, `*/Utils/`: feature-specific internals
- `*/Tests/` or `*.Tests/`: automated tests

## Build, Test, and Development Commands
Run commands from the repository root:
- `dotnet restore` - restore NuGet dependencies
- `dotnet build Listen2MeRefined.sln -c Debug` - build all projects for development
- `dotnet test Listen2MeRefined.sln -c Debug` - run all tests
- `dotnet run --project Listen2MeRefined.WPF` - launch the app locally

Use `-c Release` for production-like validation.

## Coding Style & Naming Conventions
- Use 4-space indentation and UTF-8 text files.
- Prefer file-scoped namespaces where already used.
- Naming:
  - `PascalCase` for types, methods, and public properties
  - `camelCase` for locals/parameters
  - `_camelCase` for private fields
  - Interfaces start with `I` (for example, `IWindowManager`)
- Keep classes focused; one primary responsibility per class.
- Run formatter before PRs: `dotnet format`.

## UI Reuse Rules (WPF)
- Prefer reusable components from `Listen2MeRefined.WPF/Views/Components/` when building or updating UI.
- Use existing components first: `TitleBar`, `SectionHeader`, `WindowFooterBar`, `LabeledSliderRow`, `MetaBadgeRow`, `FormFieldRow`.
- Do not add local `<Style>`, `<ControlTemplate>`, or local resource dictionaries in view/component XAML files.
- Keep shared styles in `Listen2MeRefined.WPF/Styles/ComponentStyles.xaml` or `Listen2MeRefined.WPF/Styles/ControlStyles.xaml`.
- In view XAML, keep only structure/layout and bindings (for example grid placement, row/column definitions, commands, and data bindings).
- If a visual property is VM-bound and there is no clear/simple style-based alternative, it may remain local.

## Testing Guidelines
- Use the existing .NET test framework in this repo (commonly xUnit/NUnit/MSTest).
- Mirror production namespaces in test folders.
- Test naming convention: `MethodName_State_ExpectedResult`.
- Prefer deterministic tests (avoid timing and machine-specific assumptions).
- Run: `dotnet test` before every commit.

## Commit & Pull Request Guidelines
- Write concise, imperative commit subjects (for example, `Fix keyboard hook disposal leak`).
- Keep commits scoped to one logical change.
- PRs should include:
  - what changed and why
  - linked issue/task ID (if available)
  - test evidence (`dotnet test` result)
  - screenshots/GIFs for UI changes

## Security & Configuration Tips
- Do not commit secrets, API keys, or machine-local paths.
- Keep environment-specific settings out of source control.
- Review changes in system-level hooks and input handling carefully; these areas are high impact.

## Recent Validation Findings (2026-02-20)
- `dotnet restore` can fail with `NU1301` SSL/credential errors against `api.nuget.org`; clearing NuGet locals resolved it in this environment: `dotnet nuget locals all --clear`.
- Using these env vars helped avoid first-run/permission noise during CLI validation:
  - `DOTNET_CLI_HOME=<repo>\\.dotnet-cli`
  - `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1`
- `dotnet build` at solution-level may fail in this environment due existing `NU1701` package compatibility warnings being treated as fatal.
- Project-level validation succeeded with `-p:NoWarn=NU1701`:
  - `Listen2MeRefined.Infrastructure`
  - `Listen2MeRefined.WPF`
  - `Listen2MeRefined.Tests`
- Tests passed after the settings overhaul:
  - Full suite: `31/31` passed.
  - New targeted tests (`GlobalHookStartupTaskTests`, `PlayerControlsViewModelTests`): `5/5` passed.
- Pre-existing warnings remain (not introduced by the settings overhaul), including `PresentationCore` reference warning and nullability warnings in legacy files.
