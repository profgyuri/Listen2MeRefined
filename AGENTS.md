# Repository Guidelines

## Project Structure & Module Organization
`Listen2MeRefined.sln` is the solution entry point. Keep code grouped by responsibility:
- `Listen2MeRefined.WPF/`: desktop UI (views, view models, XAML components, DI modules).
- `Listen2MeRefined.Infrastructure/`: persistence, repositories, media/system integrations, utilities.
- `Listen2MeRefined.Application/` and `Listen2MeRefined.Core/`: shared/domain and application logic.
- `Listen2MeRefined.Tests/`: xUnit test project mirroring production namespaces.

Prefer reusable contracts in Infrastructure for cross-ViewModel behavior and register implementations through `Listen2MeRefined.WPF/Dependency/Modules/UtilsModule.cs`.

## Build, Test, and Development Commands
Run from repository root:
- `dotnet restore` - restore NuGet packages.
- `dotnet build Listen2MeRefined.sln -c Debug` - build all projects.
- `dotnet test Listen2MeRefined.sln -c Debug` - run full test suite.
- `dotnet run --project Listen2MeRefined.WPF` - start the desktop app locally.

For release validation, use `-c Release`.

## Coding Style & Naming Conventions
- Use 4-space indentation and UTF-8 files.
- Keep existing file-scoped namespaces where already used.
- Naming: `PascalCase` (types/methods/properties), `camelCase` (locals/parameters), `_camelCase` (private fields), `I*` for interfaces.
- Keep classes focused on one primary responsibility.
- Run `dotnet format` before opening a PR.

## Testing Guidelines
- Framework: xUnit.
- Test names: `MethodName_State_ExpectedResult`.
- Keep tests deterministic; avoid timing-sensitive or machine-specific assumptions.
- Prefer project-level runs when troubleshooting:
  `dotnet test Listen2MeRefined.Tests/Listen2MeRefined.Tests.csproj -c Debug`.

## Commit & Pull Request Guidelines
- Use concise, imperative commit subjects (example: `Fix startup hook disposal`).
- Keep each commit scoped to one logical change.
- PRs should include: summary of what/why, linked issue or task, test evidence (`dotnet test` output), and screenshots/GIFs for UI changes.

## Security & Configuration Tips
- Never commit secrets, API keys, or machine-local paths.
- Keep environment-specific settings out of source control.
- If restore fails with `NU1301`, clear NuGet cache with `dotnet nuget locals all --clear` and retry.
