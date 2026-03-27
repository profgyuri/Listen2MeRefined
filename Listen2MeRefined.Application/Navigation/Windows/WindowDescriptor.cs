namespace Listen2MeRefined.Application.Navigation.Windows;

/// <summary>
/// Tracks a live window managed by <see cref="IWindowManager"/>.
/// </summary>
/// <param name="Window">The platform window object (typed as <c>object</c> to keep Application layer platform-agnostic).</param>
/// <param name="ShellViewModel">The shell ViewModel instance that owns the window's content.</param>
/// <param name="Context">The isolated per-shell context wired to this window.</param>
public sealed record WindowDescriptor(
    object Window,
    object ShellViewModel,
    ShellContext Context);
