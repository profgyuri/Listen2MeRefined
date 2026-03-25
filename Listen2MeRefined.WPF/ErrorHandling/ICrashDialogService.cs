using Listen2MeRefined.Application.ErrorHandling;

namespace Listen2MeRefined.WPF.ErrorHandling;

/// <summary>
/// Shows a crash-report dialog for unhandled exceptions.
/// </summary>
public interface ICrashDialogService
{
    /// <summary>
    /// Displays the crash dialog and returns the user-selected action.
    /// </summary>
    /// <param name="exception">The unhandled exception being reported.</param>
    /// <param name="context">Structured metadata describing the unhandled error source.</param>
    /// <param name="cancellationToken">A token that can cancel showing the dialog.</param>
    /// <returns>The action selected by the user in the crash dialog.</returns>
    Task<CrashDialogAction> ShowAsync(
        Exception exception,
        UnhandledErrorContext context,
        CancellationToken cancellationToken = default);
}
