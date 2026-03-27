namespace Listen2MeRefined.Application.ErrorHandling;

/// <summary>
/// Handles recoverable runtime errors in UI workflows.
/// </summary>
public interface IErrorHandler
{
    /// <summary>
    /// Handles an exception with a contextual label.
    /// </summary>
    /// <param name="exception">The exception to handle.</param>
    /// <param name="context">A short context describing where the exception occurred.</param>
    /// <param name="cancellationToken">A token that can cancel the handling operation.</param>
    /// <returns>A task representing the asynchronous handling operation.</returns>
    Task HandleAsync(Exception exception, string context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handles an unhandled exception raised by a global or fatal pipeline.
    /// </summary>
    /// <param name="exception">The unhandled exception to report.</param>
    /// <param name="context">Structured metadata describing the unhandled source.</param>
    /// <param name="cancellationToken">A token that can cancel the handling operation.</param>
    /// <returns>A task representing the asynchronous handling operation.</returns>
    Task HandleUnhandledAsync(
        Exception exception,
        UnhandledErrorContext context,
        CancellationToken cancellationToken = default);
}
