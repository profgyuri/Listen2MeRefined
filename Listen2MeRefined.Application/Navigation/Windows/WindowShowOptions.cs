namespace Listen2MeRefined.Application.Navigation.Windows;

/// <summary>
/// Carries the display configuration for a secondary window.
/// </summary>
/// <param name="Left">
/// Optional absolute screen X position to centre the window on.
/// When <see langword="null"/> the platform default is used.
/// </param>
/// <param name="Top">
/// Optional absolute screen Y position to centre the window on.
/// When <see langword="null"/> the platform default is used.
/// </param>
/// <param name="IsModal">
/// When <see langword="true"/> the window is shown as a dialog (blocking);
/// when <see langword="false"/> it is shown modeless.
/// </param>
public sealed record WindowShowOptions(
    double? Left = null,
    double? Top = null,
    bool IsModal = true)
{
    /// <summary>
    /// Centres the window on the main window of the application.
    /// </summary>
    public static WindowShowOptions CenteredOnMainWindow(bool isModal = true)
        => new WindowShowOptions(Left: null, Top: null, IsModal: isModal) with { CentreOnMainWindow = true };
 
    /// <summary>
    /// Positions the window centred on the given screen point.
    /// </summary>
    public static WindowShowOptions At(double left, double top, bool isModal = true)
        => new(Left: left, Top: top, IsModal: isModal);
 
    /// <summary>
    /// Shows a modeless window at the platform-default position.
    /// </summary>
    public static WindowShowOptions Modeless()
        => new(IsModal: false);
 
    // Internal flag used by the WPF implementation — callers use the factory
    // methods above rather than setting this directly.
    public bool CentreOnMainWindow { get; private init; }
}
