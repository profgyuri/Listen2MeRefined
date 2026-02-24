namespace Listen2MeRefined.Infrastructure.Settings.WindowPosition;

/// <summary>
/// Provides policy mapping for window-position related options.
/// </summary>
public interface IWindowPositionPolicyService
{
    /// <summary>Determines whether the window should be topmost for a given position setting.</summary>
    bool IsTopmost(string? windowPosition);
}
