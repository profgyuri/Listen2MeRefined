using Listen2MeRefined.Application.ViewModels.Shells;
using Listen2MeRefined.Application.ViewModels;

namespace Listen2MeRefined.Application.Navigation.Windows;

/// <summary>
/// Manages the creation, positioning, and lifecycle of application windows.
/// Callers are responsible for all window-specific logic; this contract
/// remains generic and Open-Closed with respect to new window types.
/// </summary>
public interface IWindowManager
{
    /// <summary>
    /// Displays the primary application window, wiring it as
    /// <see cref="System.Windows.Application.MainWindow"/>.
    /// </summary>
    /// <typeparam name="TShellViewModel">
    /// The shell ViewModel that drives the window's content area.
    /// </typeparam>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    Task ShowMainWindowAsync<TShellViewModel>(
        CancellationToken cancellationToken = default)
        where TShellViewModel : ShellViewModelBase;
 
    /// <summary>
    /// Opens a non-main window, optionally positioned relative to a point
    /// on screen.
    /// </summary>
    /// <typeparam name="TShellViewModel">
    /// The shell ViewModel that drives the window's content area.
    /// </typeparam>
    /// <param name="options">Positioning and modality settings.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>
    /// The dialog result when <paramref name="options"/> requests a modal
    /// window; <see langword="null"/> for modeless windows.
    /// </returns>
    Task<bool?> ShowWindowAsync<TShellViewModel>(
        WindowShowOptions options,
        CancellationToken cancellationToken = default)
        where TShellViewModel : ShellViewModelBase;

    /// <summary>
    /// Opens the popup shell and displays the popup content view model selected by
    /// <typeparamref name="TPopupViewModel"/>.
    /// </summary>
    /// <typeparam name="TPopupViewModel">The popup content view model type.</typeparam>
    /// <param name="options">Positioning and modality settings.</param>
    /// <param name="configureViewModel">
    /// Optional callback invoked after navigation succeeds and before the popup is shown.
    /// </param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>
    /// The dialog result when <paramref name="options"/> requests a modal
    /// popup; <see langword="null"/> for modeless popups.
    /// </returns>
    Task<bool?> ShowPopupAsync<TPopupViewModel>(
        WindowShowOptions options,
        Action<TPopupViewModel>? configureViewModel = null,
        CancellationToken cancellationToken = default)
        where TPopupViewModel : PopupViewModelBase;
 
    /// <summary>
    /// Closes and disposes a previously opened window identified by its
    /// shell ViewModel instance.
    /// </summary>
    /// <typeparam name="TShellViewModel">
    /// The shell ViewModel whose window should be closed.
    /// </typeparam>
    void CloseWindow<TShellViewModel>() where TShellViewModel : ShellViewModelBase;
 
    /// <summary>
    /// Returns <see langword="true"/> when at least one window driven by the
    /// specified shell ViewModel type is currently open.
    /// </summary>
    /// <typeparam name="TShellViewModel">The shell ViewModel type to check.</typeparam>
    bool IsOpen<TShellViewModel>() where TShellViewModel : ShellViewModelBase;
}
 
