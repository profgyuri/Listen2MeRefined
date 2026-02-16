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
